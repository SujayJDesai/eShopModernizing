using eShopModernizedWebForms.Components;
using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Models.Infrastructure;
using eShopModernizedWebForms.Modules;
using eShopModernizedWebForms.Services;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Razor Components (Blazor Server)
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---------------------------------------------------------------------------
// MVC controllers — needed for Microsoft.Identity.Web.UI sign-in/sign-out
// endpoints (MicrosoftIdentity/Account/SignIn, SignOut).
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// ---------------------------------------------------------------------------
// Application Insights — replaces TelemetryConfiguration.Active / AI HTTP
// modules (TelemetryCorrelationHttpModule, ApplicationInsightsHttpModule).
// ---------------------------------------------------------------------------
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ApplicationVersion = "1.0.0";
});

// Cloud role name — set via environment variable OTEL_SERVICE_NAME or
// OTEL_RESOURCE_ATTRIBUTES="service.name=eShopModernizedWebForms".
// The App Insights 3.x SDK (OpenTelemetry-based) reads it from there.
Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "eShopModernizedWebForms");

// ---------------------------------------------------------------------------
// Logging — replaces log4net (Microsoft.Extensions.Logging is built in).
// ---------------------------------------------------------------------------
builder.Logging.AddConsole();

// ---------------------------------------------------------------------------
// Configuration-based feature toggles (replaces CatalogConfiguration /
// ConfigurationManager.AppSettings).  Values come from appsettings.json,
// environment variables, user-secrets, or Azure Key Vault (see below).
// ---------------------------------------------------------------------------
bool useMockData = builder.Configuration.GetValue<bool>("UseMockData");
bool useAzureStorage = builder.Configuration.GetValue<bool>("UseAzureStorage");
bool useManagedIdentity = builder.Configuration.GetValue<bool>("UseAzureManagedIdentity");
bool useAzureActiveDirectory = builder.Configuration.GetValue<bool>("UseAzureActiveDirectory");

// ---------------------------------------------------------------------------
// Azure Key Vault configuration provider — replaces the legacy
// OptionalKeyVaultConfigurationBuilder from Web.config <configBuilders>.
// ---------------------------------------------------------------------------
string? keyVaultName = builder.Configuration["KeyVaultName"];
if (!string.IsNullOrEmpty(keyVaultName))
{
    var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new Azure.Identity.DefaultAzureCredential());
}

// ---------------------------------------------------------------------------
// Entity Framework Core — replaces EF6 + Database.SetInitializer.
// CatalogDBContext now takes DbContextOptions (EF Core).
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

// ---------------------------------------------------------------------------
// Application services — replaces Autofac ContainerBuilder / ApplicationModule.
// ApplicationModule.AddApplicationServices is the ported extension method.
// ---------------------------------------------------------------------------
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddApplicationServices(useMockData, useAzureStorage, useManagedIdentity);

// ---------------------------------------------------------------------------
// Authentication — replaces OWIN cookie + OpenIdConnect (Startup.Auth.cs).
// Microsoft.Identity.Web wraps AddAuthentication + AddOpenIdConnect + AddCookie.
// Only enabled when UseAzureActiveDirectory is true (mirrors legacy behaviour).
// ---------------------------------------------------------------------------
if (useAzureActiveDirectory)
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
            options.ClientId = builder.Configuration["AzureActiveDirectoryClientId"];
            options.TenantId = builder.Configuration["AzureActiveDirectoryTenant"];
            options.Instance = builder.Configuration["AzureActiveDirectoryInstance"]
                               ?? "https://login.microsoftonline.com/";
            options.CallbackPath = "/signin-oidc";
            options.SignedOutCallbackPath = "/signout-callback-oidc";
        });
}
else
{
    // When AAD is disabled the app runs without authentication; AddAuthentication
    // is still registered so AuthorizeView / [Authorize] resolve gracefully (no
    // scheme = anonymous access allowed).
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// ---------------------------------------------------------------------------
// Session & HttpContext — replaces Session_Start / HttpContext.Current.
// ---------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// ---------------------------------------------------------------------------
// Catalog DB seeding — replaces EF6 Database.SetInitializer<CatalogDBContext>
// (CreateDatabaseIfNotExists + Seed).  Runs once at startup via a hosted
// service before the image initializer.
// ---------------------------------------------------------------------------
builder.Services.AddHostedService<CatalogDBSeedingService>();

// ---------------------------------------------------------------------------
// Catalog image initialization — replaces InitializeCatalogImages() called
// at Application_Start.  Runs once at startup via a hosted service so it does
// not block the request pipeline.
// ---------------------------------------------------------------------------
builder.Services.AddHostedService<CatalogImageInitializerService>();

// ---------------------------------------------------------------------------
// Build the app
// ---------------------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline — order is load-bearing (see ASP.NET Core docs).
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Application_Error → centralised exception logging.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception");
        throw; // re-throw so UseExceptionHandler can handle the response
    }
});

// Application_BeginRequest → request logging middleware (replaces log4net
// LogicalThreadContext properties and Trace.TraceInformation).
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogDebug("Received request {Url}", context.Request.GetDisplayUrl());
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseAntiforgery();

// ---------------------------------------------------------------------------
// Microsoft Identity Web endpoints (sign-in / sign-out for OpenIdConnect).
// These handle the /MicrosoftIdentity/Account/SignIn and SignOut links from
// the MainLayout header.
// ---------------------------------------------------------------------------
app.MapControllers();

// ---------------------------------------------------------------------------
// Blazor Server endpoints — modern .NET 8+ pattern (NOT MapBlazorHub /
// MapFallbackToPage("/_Host")).
// ---------------------------------------------------------------------------
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ---------------------------------------------------------------------------
// Hosted service: catalog image initialization (was InitializeCatalogImages
// in Application_Start).  Resolves IImageService from a scoped service
// provider.
// ---------------------------------------------------------------------------
public sealed class CatalogImageInitializerService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CatalogImageInitializerService> _logger;

    public CatalogImageInitializerService(IServiceScopeFactory scopeFactory,
        ILogger<CatalogImageInitializerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing catalog images...");
        using var scope = _scopeFactory.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        imageService.InitializeCatalogImages();
        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// ---------------------------------------------------------------------------
// Hosted service: catalog DB seeding (replaces EF6
// Database.SetInitializer<CatalogDBContext> + CreateDatabaseIfNotExists).
// Runs EnsureCreated then seeds via CatalogDBInitializer.
// ---------------------------------------------------------------------------
public sealed class CatalogDBSeedingService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CatalogDBSeedingService> _logger;

    public CatalogDBSeedingService(IServiceScopeFactory scopeFactory,
        ILogger<CatalogDBSeedingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding catalog database...");
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
        context.Database.EnsureCreated();
        var initializer = scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>();
        initializer.Seed(context);
        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
