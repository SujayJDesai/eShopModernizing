using eShopWCFService.Models;
using Microsoft.EntityFrameworkCore;

namespace eShopWCFService
{
    /// <summary>
    /// Catalog service implementation.
    /// Formerly WCF CatalogService.svc.cs; now a scoped service registered in DI
    /// and consumed by CatalogController.
    /// </summary>
    public class CatalogService : ICatalogService
    {
        private readonly EntityModel _db;

        public CatalogService(EntityModel db)
        {
            _db = db;
        }

        public DiscountItem GetDiscount(DateTime day)
        {
            // FIX: Original called .ToList() before .Where() — now filters server-side.
            var dayDate = day.Date;
            return _db.DiscountItems
                .Where(y => y.Start.Date <= dayDate && y.End.Date >= dayDate)
                .FirstOrDefault();
        }

        public CatalogItem FindCatalogItem(int id)
        {
            return _db.CatalogItems
                .Include(ci => ci.CatalogBrand)
                .Include(ci => ci.CatalogType)
                .FirstOrDefault(x => x.Id == id);
        }

        public List<CatalogType> GetCatalogTypes()
        {
            return _db.CatalogTypes.ToList();
        }

        public List<CatalogBrand> GetCatalogBrands()
        {
            return _db.CatalogBrands.ToList();
        }

        public List<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter)
        {
            // FIX: Original called .ToList() before .Where() — now filters server-side.
            IQueryable<CatalogItem> query = _db.CatalogItems;

            if (brandIdFilter != 0)
                query = query.Where(x => x.CatalogBrandId == brandIdFilter);

            if (typeIdFilter != 0)
                query = query.Where(x => x.CatalogTypeId == typeIdFilter);

            return query.ToList();
        }

        public void CreateCatalogItem(CatalogItem catalogItem)
        {
            // FIX: Removed manual Max(Id)+1 — relies on database identity column
            // ([DatabaseGenerated(DatabaseGeneratedOption.Identity)] on CatalogItem.Id).
            catalogItem.Id = 0;
            _db.CatalogItems.Add(catalogItem);
            _db.SaveChanges();
        }

        public void UpdateCatalogItem(CatalogItem catalogItem)
        {
            _db.Entry(catalogItem).State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void RemoveCatalogItem(CatalogItem catalogItem)
        {
            _db.CatalogItems.Remove(catalogItem);
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public int GetAvailableStock(DateTime date, int catalogItemId)
        {
            // FIX: Original called .ToList() before second .Where() — now filters server-side.
            var targetDate = date.Date;
            var stock = _db.CatalogItemsStocks
                .Where(x => x.CatalogItemId == catalogItemId && x.Date.Date == targetDate)
                .FirstOrDefault();

            return stock?.AvailableStock ?? 0;
        }

        public void CreateAvailableStock(CatalogItemsStock catalogItemsStock)
        {
            // FIX: Moved filtering server-side and removed manual Max(StockId)+1.
            var targetDate = catalogItemsStock.Date.Date;
            var existing = _db.CatalogItemsStocks
                .Where(x => x.CatalogItemId == catalogItemsStock.CatalogItemId
                             && x.Date.Date == targetDate)
                .FirstOrDefault();

            if (existing != null)
            {
                // Overwrite the existing stock item for that date.
                existing.AvailableStock = catalogItemsStock.AvailableStock;
                _db.Entry(existing).State = EntityState.Modified;
                _db.SaveChanges();
            }
            else
            {
                // Let the database assign the identity via [DatabaseGenerated(DatabaseGeneratedOption.Identity)].
                catalogItemsStock.StockId = 0;
                _db.CatalogItemsStocks.Add(catalogItemsStock);
                _db.SaveChanges();
            }
        }
    }
}
