using eShopLegacyWebForms.Models;
using eShopLegacyWebForms.Models.Infrastructure;
using eShopLegacyWebForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace eShopLegacyWebForms.Modules
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