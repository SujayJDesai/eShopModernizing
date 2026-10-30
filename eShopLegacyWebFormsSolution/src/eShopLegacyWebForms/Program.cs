using eShopLegacyWebForms.Components;
using eShopLegacyWebForms.Models;
using eShopLegacyWebForms.Models.Infrastructure;
using eShopLegacyWebForms.Modules;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Application Insights telemetry (replaces legacy AI HTTP modules:
// TelemetryCorrelationHttpModule and ApplicationInsightsWebTracking).
builder.Services.AddApplicationInsightsTelemetry();

// Blazor Server components.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// EF Core DbContext — connection string from appsettings.json.
builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

// Application services (ICatalogService, CatalogDBInitializer, CatalogItemHiLoGenerator).
// The ApplicationModule was already converted from Autofac to an IServiceCollection extension.
var useMockData = bool.Parse(builder.Configuration["UseMockData"] ?? "true");
builder.Services.AddApplicationServices(useMockData);

// Session support — replaces Session_Start tracking (MachineName, SessionStartTime).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Database initialization — replaces ConfigDataBase() from Application_Start.
if (!useMockData)
{
    using var scope = app.Services.CreateScope();
    var dbInitializer = scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
    dbInitializer.Seed(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Request-logging middleware — replaces Application_BeginRequest.
// The legacy handler attached an activity-correlation ID and request info to log4net context;
// ILogger with Activity tracing provides the same correlation automatically.
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var activityId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["ActivityId"] = activityId,
        ["RequestUrl"] = context.Request.Path.ToString(),
        ["UserAgent"] = context.Request.Headers.UserAgent.ToString()
    }))
    {
        logger.LogDebug("BeginRequest {RequestUrl}", context.Request.Path);
        await next();
    }
});

// Session middleware — replaces Session_Start.
// Session_Start used to set MachineName and SessionStartTime on the session;
// components that need these values can read them from HttpContext.Session or IHttpContextAccessor.
app.UseSession();
app.Use(async (context, next) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("MachineName")))
    {
        context.Session.SetString("MachineName", Environment.MachineName);
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString("o"));
    }
    await next();
});

app.UseRouting();
app.UseAntiforgery();

// [PORT-NOTE] Dropped: RouteConfig.RegisterRoutes — Web Forms route table is not used in Blazor Server.
// [PORT-NOTE] Dropped: BundleConfig.RegisterBundles — Web.Optimization bundling is not supported on ASP.NET Core;
//   static assets serve directly from wwwroot/.
// [PORT-NOTE] Dropped: Autofac ContainerProvider — replaced by built-in DI above.
// [PORT-NOTE] Dropped: log4net (LogicalThreadContext, ILog) — replaced by ILogger<T> and structured logging middleware.
// [PORT-NOTE] Dropped: ActivityIdHelper / WebRequestInfo helper classes — Activity.Current and ILogger scope provide
//   equivalent correlation and request-info logging.
// [PORT-NOTE] Dropped: Four HTTP modules from Web.config (ContainerDisposal, PropertyInjection,
//   TelemetryCorrelationHttpModule, ApplicationInsightsWebTracking) — all replaced by ASP.NET Core
//   built-in DI, Application Insights middleware, and the request pipeline above.

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
