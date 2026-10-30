using eShopWCFService;
using eShopWCFService.Models.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register controllers (replaces WCF ServiceHost / system.serviceModel).
builder.Services.AddControllers();

// Register EntityModel as a scoped DbContext using the connection string from appsettings.json.
// This replaces the legacy CatalogConfiguration.ConnectionString helper and WCF per-call instantiation.
builder.Services.AddDbContext<EntityModel>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EntityModel")));

// Register CatalogService as scoped (mirrors WCF PerCall instance context mode).
builder.Services.AddScoped<CatalogService>();

// Seed the database on startup.
builder.Services.AddHostedService<CatalogDBInitializer>();

// Register CatalogServiceClient as a typed HttpClient (replaces WCF ClientBase<ICatalogService>).
builder.Services.AddHttpClient<CatalogServiceClient>();

// Swagger / OpenAPI for development.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
