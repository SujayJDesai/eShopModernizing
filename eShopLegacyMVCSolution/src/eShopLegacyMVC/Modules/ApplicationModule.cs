using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Services;
using Microsoft.Extensions.DependencyInjection;

namespace eShopLegacyMVC.Modules
{
    public static class ApplicationModule
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, bool useMockData)
        {
            if (useMockData)
            {
                services.AddSingleton<ICatalogService, CatalogServiceMock>();
            }
            else
            {
                services.AddScoped<ICatalogService, CatalogService>();
            }

            services.AddScoped<CatalogDBContext>();

            services.AddScoped<CatalogDBInitializer>();

            services.AddSingleton<CatalogItemHiLoGenerator>();

            return services;
        }
    }
}