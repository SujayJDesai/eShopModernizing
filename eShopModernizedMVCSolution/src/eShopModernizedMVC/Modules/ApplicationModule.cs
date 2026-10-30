using eShopModernizedMVC.Models;
using eShopModernizedMVC.Models.Infrastructure;
using eShopModernizedMVC.Services;
using Microsoft.Extensions.DependencyInjection;

namespace eShopModernizedMVC.Modules
{
    public static class ApplicationModule
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            bool useMockData,
            bool useAzureStorage,
            bool useManagedIdentity)
        {
            if (useMockData)
            {
                services.AddSingleton<ICatalogService, CatalogServiceMock>();
            }
            else
            {
                services.AddScoped<ICatalogService, CatalogService>();
            }

            if (useAzureStorage)
            {
                services.AddScoped<IImageService, ImageAzureStorage>();
            }
            else
            {
                services.AddScoped<IImageService, ImageMockStorage>();
            }

            services.AddScoped<CatalogDBContext>();

            services.AddScoped<CatalogDBInitializer>();

            services.AddSingleton<CatalogItemHiLoGenerator>();

            return services;
        }
    }
}