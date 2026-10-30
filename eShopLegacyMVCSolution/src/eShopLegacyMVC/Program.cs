using System.Diagnostics;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------

// MVC controllers (unified MVC 5 + Web API 2 into single controller model)
builder.Services.AddControllersWithViews();

// Application Insights telemetry (replaces the 7 legacy AI packages)
builder.Services.AddApplicationInsightsTelemetry();

// EF Core DbContext — replaces EF6 CatalogDBContext + Database.SetInitializer
builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

// Application services from the former Autofac ApplicationModule
var useMockData = bool.Parse(builder.Configuration["UseMockData"] ?? "false");
builder.Services.AddApplicationServices(useMockData);

// HttpContextAccessor — replaces HttpContext.Current used by ActivityIdHelper / WebRequestInfo
builder.Services.AddHttpContextAccessor();

// Session — replaces InProc session (sessionState mode=InProc)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Logging (ILogger<T> already injected throughout; the framework wires it automatically)
// [PORT-NOTE] log4net replaced by built-in ILogger<T> — no explicit log4net configuration needed.

var app = builder.Build();

// ---------------------------------------------------------------------------
// Database initialization — replaces Database.SetInitializer<CatalogDBContext> + ConfigDataBase()
// ---------------------------------------------------------------------------
if (!useMockData)
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
        dbContext.Database.EnsureCreated();

        var dbInitializer = scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>();
        dbInitializer.Seed(dbContext);
    }
}

// ---------------------------------------------------------------------------
// Middleware pipeline (order is load-bearing — see ASP.NET Core middleware order docs)
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

app.UseHttpsRedirection();
app.UseStaticFiles();

// Request logging middleware — replaces Application_BeginRequest (ActivityIdHelper + WebRequestInfo)
app.Use(async (context, next) =>
{
    // ActivityIdHelper: ensure a correlation activity ID is set for every request
    if (Trace.CorrelationManager.ActivityId == Guid.Empty)
    {
        Trace.CorrelationManager.ActivityId = Guid.NewGuid();
    }

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var activityId = Trace.CorrelationManager.ActivityId.ToString();
    var requestInfo = $"{context.Request.Path}{context.Request.QueryString}, {context.Request.Headers.UserAgent}";

    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["ActivityId"] = activityId,
        ["RequestInfo"] = requestInfo
    }))
    {
        logger.LogDebug("BeginRequest");
        await next();
    }
});

// Session middleware — replaces Session_Start handler
app.UseSession();

app.UseRouting();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// Endpoint mapping
// ---------------------------------------------------------------------------

// Default MVC route — from RouteConfig: controller=Catalog, action=Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

// [PORT-NOTE] WebApiConfig.Register (api/{controller}/{id}) is no longer needed.
// Web API 2 controllers (FilesController, BrandsController, Api/CatalogController)
// now use attribute routing ([Route], [ApiController]) via MapControllerRoute + MapControllers.

// [PORT-NOTE] AreaRegistration.RegisterAllAreas() dropped — no areas defined in this project.

// [PORT-NOTE] BundleConfig.RegisterBundles dropped — Web.Optimization bundles are replaced by
// direct link/script references to static assets in wwwroot/ (Content/, Scripts/).

// [PORT-NOTE] FilterConfig.RegisterGlobalFilters (HandleErrorAttribute) replaced by
// UseExceptionHandler / UseDeveloperExceptionPage middleware above.

app.Run();
