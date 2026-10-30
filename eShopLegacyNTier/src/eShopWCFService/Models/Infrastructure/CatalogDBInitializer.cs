using eShopWCFService;
using eShopWCFService.Models.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace eShopWCFService.Models.Infrastructure
{
    public class CatalogDBInitializer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public CatalogDBInitializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EntityModel>();
            await context.Database.EnsureCreatedAsync(cancellationToken);
            await SeedAsync(context, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task SeedAsync(EntityModel context, CancellationToken cancellationToken)
        {
            await AddCatalogTypesAsync(context, cancellationToken);
            await AddCatalogBrandsAsync(context, cancellationToken);
            await AddCatalogItemsAsync(context, cancellationToken);
            await AddCatalogItemsStockAsync(context, cancellationToken);
            await AddDiscountItemsAsync(context, cancellationToken);
        }

        private async Task AddCatalogTypesAsync(EntityModel context, CancellationToken cancellationToken)
        {
            if (await context.CatalogTypes.AnyAsync(cancellationToken))
                return;

            var preconfiguredTypes = PreconfiguredData.GetPreconfiguredCatalogTypes();

            foreach (var type in preconfiguredTypes)
            {
                context.CatalogTypes.Add(type);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task AddCatalogBrandsAsync(EntityModel context, CancellationToken cancellationToken)
        {
            if (await context.CatalogBrands.AnyAsync(cancellationToken))
                return;

            var preconfiguredBrands = PreconfiguredData.GetPreconfiguredCatalogBrands();

            foreach (var brand in preconfiguredBrands)
            {
                context.CatalogBrands.Add(brand);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task AddDiscountItemsAsync(EntityModel context, CancellationToken cancellationToken)
        {
            if (await context.DiscountItems.AnyAsync(cancellationToken))
                return;

            var preconfiguredDiscounts = PreconfiguredData.GetPreconfiguredDiscountItems();

            foreach (var discount in preconfiguredDiscounts)
            {
                context.DiscountItems.Add(discount);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task AddCatalogItemsAsync(EntityModel context, CancellationToken cancellationToken)
        {
            if (await context.CatalogItems.AnyAsync(cancellationToken))
                return;

            var preconfiguredItems = PreconfiguredData.GetPreconfiguredCatalogItems();

            foreach (var item in preconfiguredItems)
            {
                context.CatalogItems.Add(item);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task AddCatalogItemsStockAsync(EntityModel context, CancellationToken cancellationToken)
        {
            if (await context.CatalogItemsStocks.AnyAsync(cancellationToken))
                return;

            var preconfiguredStock = PreconfiguredData.GetPreconfiguredCatalogItemsStock();

            foreach (var s in preconfiguredStock)
            {
                context.CatalogItemsStocks.Add(s);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}