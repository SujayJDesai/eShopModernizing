using Amazon;
using Amazon.S3;
using eShopModernizedMVC;
using eShopModernizedMVC.Filters;
using eShopModernizedMVC.Models;
using eShopModernizedMVC.Models.Infrastructure;
using eShopModernizedMVC.Modules;
using eShopModernizedMVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration bridge: make IConfiguration available to CatalogConfiguration
// (which still uses static accessors consumed across the app).
// ---------------------------------------------------------------------------
CatalogConfiguration.Configuration = builder.Configuration;

// ---------------------------------------------------------------------------
// Application Insights telemetry (replaces InitializePipeline / MyTelemetryInitializer)
// ---------------------------------------------------------------------------
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    var aiKey = builder.Configuration["AppInsightsInstrumentationKey"];
    if (!string.IsNullOrEmpty(aiKey))
    {
        options.ConnectionString = aiKey;
    }
});

// ---------------------------------------------------------------------------
// MVC + global filters (FilterConfig.RegisterGlobalFilters → inline)
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    // ActionTracerFilter: traces action entry
    options.Filters.Add<ActionTracerFilter>();

    // HandleErrorAttribute equivalent: ASP.NET Core uses UseExceptionHandler middleware (configured below).

    // OutputCache { VaryByParam = "*", Duration = 0, NoStore = true } → ResponseCache no-store on every response
    options.Filters.Add(new ResponseCacheAttribute
    {
        NoStore = true,
        Duration = 0,
        Location = ResponseCacheLocation.None
    });
});

// ---------------------------------------------------------------------------
// EF Core – CatalogDBContext (replaces EF6 Database.SetInitializer)
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<CatalogDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext"));
});

// ---------------------------------------------------------------------------
// Amazon S3 client (replaces Azure Blob Storage BlobServiceClient)
// ---------------------------------------------------------------------------
if (CatalogConfiguration.UseAzureStorage)
{
    var regionName = builder.Configuration["AWS_REGION"]
                     ?? Environment.GetEnvironmentVariable("AWS_REGION");
    if (string.IsNullOrWhiteSpace(regionName))
    {
        throw new InvalidOperationException(
            "AWS_REGION must be set (configuration or environment variable) when UseAzureStorage is enabled.");
    }
    builder.Services.AddSingleton<IAmazonS3>(
        _ => new AmazonS3Client(RegionEndpoint.GetBySystemName(regionName)));
}

// ---------------------------------------------------------------------------
// Application services (replaces Autofac RegisterContainer / ApplicationModule)
// ---------------------------------------------------------------------------
builder.Services.AddApplicationServices(
    useMockData: CatalogConfiguration.UseMockData,
    useAzureStorage: CatalogConfiguration.UseAzureStorage,
    useManagedIdentity: CatalogConfiguration.UseManagedIdentity);

// ---------------------------------------------------------------------------
// Session (replaces Session_Start – the per-session values are set via middleware below)
// ---------------------------------------------------------------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// ---------------------------------------------------------------------------
// HttpContextAccessor (needed by services that formerly used HttpContext.Current)
// ---------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// Authentication – Azure AD via OpenID Connect (conditional, replaces Startup.Auth.cs ConfigureAuth)
// ---------------------------------------------------------------------------
if (CatalogConfiguration.UseAzureActiveDirectory)
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            options.ClientId = CatalogConfiguration.AzureActiveDirectoryClientId;
            var aadInstance = builder.Configuration["AzureActiveDirectoryInstance"] ?? "https://login.microsoftonline.com/{0}";
            options.Authority = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                aadInstance,
                CatalogConfiguration.AzureActiveDirectoryTenant);
            options.SignedOutRedirectUri = CatalogConfiguration.PostLogoutRedirectUri;
        });

    builder.Services.AddAuthorization();
}

// ---------------------------------------------------------------------------
// Logging (Microsoft.Extensions.Logging replaces log4net; configured in appsettings.json)
// ---------------------------------------------------------------------------

var app = builder.Build();

// ---------------------------------------------------------------------------
// Exception handling (replaces Application_Error)
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Centralised error logging that matches the legacy Application_Error behavior,
// active in ALL environments (the handler above is only for non-dev).
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception typeof({ExceptionType}): {Message}", ex.GetType().Name, ex.Message);
        throw; // re-throw so the UseExceptionHandler / UseDeveloperExceptionPage still fires
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

// ---------------------------------------------------------------------------
// Request tracing middleware (replaces Application_BeginRequest)
// ActivityIdHelper + WebRequestInfo logging is now handled via ILogger scopes.
// ---------------------------------------------------------------------------
app.Use(async (context, next) =>
{
    // Ensure an Activity ID is set for correlation (replaces ActivityIdHelper)
    if (Trace.CorrelationManager.ActivityId == Guid.Empty)
    {
        Trace.CorrelationManager.ActivityId = Guid.NewGuid();
    }

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var requestUrl = context.Request.Path + context.Request.QueryString;
    var userAgent = context.Request.Headers.UserAgent.ToString();

    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["ActivityId"] = Trace.CorrelationManager.ActivityId,
        ["RequestInfo"] = $"{requestUrl}, {userAgent}"
    }))
    {
        logger.LogDebug("WebApplication_BeginRequest");
        await next();
    }
});

app.UseRouting();

// Session middleware (replaces Session_Start)
app.UseSession();

// Seed session values on first request in a session (replaces Session_Start handler)
app.Use(async (context, next) =>
{
    if (context.Session.GetString("MachineName") == null)
    {
        context.Session.SetString("MachineName", Environment.MachineName);
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString("o"));
    }
    await next();
});

if (CatalogConfiguration.UseAzureActiveDirectory)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseAntiforgery();

// ---------------------------------------------------------------------------
// Routing (replaces RouteConfig.RegisterRoutes)
// Default route points to Catalog/Index, matching the legacy RouteConfig.
// Attribute routing is enabled by default in ASP.NET Core MVC.
// ---------------------------------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

// Serve images from Pics folder (replaces routes.IgnoreRoute("Pics/{*pathInfo}"))
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Pics")),
    RequestPath = "/Pics"
});

// ---------------------------------------------------------------------------
// Database initialisation at startup (replaces ConfigDataBase + InitializeCatalogImages)
// ---------------------------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        if (!CatalogConfiguration.UseMockData)
        {
            var dbContext = services.GetRequiredService<CatalogDBContext>();
            dbContext.Database.Migrate();

            var dbInitializer = services.GetRequiredService<CatalogDBInitializer>();
            if (!dbContext.CatalogItems.Any())
            {
                dbInitializer.Seed(dbContext);
            }
        }

        var imageService = services.GetRequiredService<IImageService>();
        imageService.InitializeCatalogImages();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure Application Insights cloud role name (replaces MyTelemetryInitializer).
// In App Insights 3.x (OpenTelemetry-based), the cloud role name is set via
// the OTEL_SERVICE_NAME environment variable or APPLICATIONINSIGHTS_ROLE_NAME.
// Setting it here as a fallback if not already configured externally.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME"))
    && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_ROLE_NAME")))
{
    Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "eShopModernizedMVC");
}

app.Run();

// ---------------------------------------------------------------------------
// Cloud Role Name for Application Insights (replaces MyTelemetryInitializer)
// SetCloudRole configures the OpenTelemetry Resource used by App Insights 3.x.
// ---------------------------------------------------------------------------
namespace eShopModernizedMVC
{
    // ---------------------------------------------------------------------------
    // CatalogConfiguration (was in App_Start/CatalogConfiguration.cs)
    // Migrated from ConfigurationManager.AppSettings to IConfiguration.
    // Retains static accessors consumed across the project (controllers, services,
    // initializers). The IConfiguration instance is assigned once at startup above.
    // ---------------------------------------------------------------------------
    public static class CatalogConfiguration
    {
        public static IConfiguration Configuration { get; set; } = default!;

        public static bool UseMockData => IsEnabled("UseMockData");

        public static bool UseAzureStorage => IsEnabled("UseAzureStorage");

        public static bool UseManagedIdentity => IsEnabled("UseAzureManagedIdentity");

        public static bool UseCustomizationData => IsEnabled("UseCustomizationData");


        public static string S3BucketName => Configuration["S3BucketName"] ?? string.Empty;

        public static string AppInsightsInstrumentationKey => Configuration["AppInsightsInstrumentationKey"] ?? string.Empty;

        public static bool UseAzureActiveDirectory => IsEnabled("UseAzureActiveDirectory");

        public static string AzureActiveDirectoryClientId => Configuration["AzureActiveDirectoryClientId"] ?? string.Empty;

        public static string AzureActiveDirectoryTenant => Configuration["AzureActiveDirectoryTenant"] ?? string.Empty;

        public static string PostLogoutRedirectUri => Configuration["PostLogoutRedirectUri"] ?? string.Empty;

        private static bool IsEnabled(string configurationKey)
        {
            var value = Configuration[configurationKey];
            return bool.TryParse(value, out var result) && result;
        }
    }
}
