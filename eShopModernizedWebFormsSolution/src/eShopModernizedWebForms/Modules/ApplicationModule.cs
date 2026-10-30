using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Models.Infrastructure;
using eShopModernizedWebForms.Services;

namespace eShopModernizedWebForms.Modules
{
    public static class ApplicationModule
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services,
            bool useMockData, bool useAzureStorage, bool useManagedIdentity)
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
                services.AddAWSService<IAmazonS3>();
                services.AddScoped<IImageService, ImageAzureStorage>();
            }
            else
            {
                services.AddScoped<IImageService, ImageMockStorage>();
            }

            services.AddScoped<CatalogDBContext>();

            services.AddScoped<CatalogDBInitializer>();

            services.AddSingleton<CatalogItemHiLoGenerator>();

            if (useManagedIdentity)
            {
                services.AddSingleton<ISqlConnectionFactory, ManagedIdentitySqlConnectionFactory>();
            }
            else
            {
                services.AddSingleton<ISqlConnectionFactory, AppSettingsSqlConnectionFactory>();
            }

            return services;
        }
    }
}