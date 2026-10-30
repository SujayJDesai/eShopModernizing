using eShopWCFService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eShopWCFService.Controllers;

/// <summary>
/// ASP.NET Core Web API controller replacing the legacy WCF CatalogService.svc.
/// Each [OperationContract] on ICatalogService is mapped to a REST endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly EntityModel _db;

    public CatalogController(EntityModel db)
    {
        _db = db;
    }

    // GET api/catalog/items/{id}
    // Was: FindCatalogItem(int id)
    [HttpGet("items/{id:int}")]
    public async Task<ActionResult<CatalogItem>> FindCatalogItem(int id)
    {
        var item = await _db.CatalogItems
            .Include(ci => ci.CatalogBrand)
            .Include(ci => ci.CatalogType)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is null)
            return NotFound();

        return Ok(item);
    }

    // GET api/catalog/brands
    // Was: GetCatalogBrands()
    [HttpGet("brands")]
    public async Task<ActionResult<List<CatalogBrand>>> GetCatalogBrands()
    {
        var brands = await _db.CatalogBrands.ToListAsync();
        return Ok(brands);
    }

    // GET api/catalog/types
    // Was: GetCatalogTypes()
    [HttpGet("types")]
    public async Task<ActionResult<List<CatalogType>>> GetCatalogTypes()
    {
        var types = await _db.CatalogTypes.ToListAsync();
        return Ok(types);
    }

    // GET api/catalog/items?brandIdFilter=0&typeIdFilter=0
    // Was: GetCatalogItems(int brandIdFilter, int typeIdFilter)
    // FIX: Original called .ToList() before .Where(), loading the full table into memory.
    //       Now applies filters server-side before materializing.
    [HttpGet("items")]
    public async Task<ActionResult<List<CatalogItem>>> GetCatalogItems(
        [FromQuery] int brandIdFilter = 0,
        [FromQuery] int typeIdFilter = 0)
    {
        IQueryable<CatalogItem> query = _db.CatalogItems;

        if (brandIdFilter != 0)
            query = query.Where(x => x.CatalogBrandId == brandIdFilter);

        if (typeIdFilter != 0)
            query = query.Where(x => x.CatalogTypeId == typeIdFilter);

        var items = await query.ToListAsync();
        return Ok(items);
    }

    // GET api/catalog/discount?day=2024-01-15
    // Was: GetDiscount(DateTime day)
    // FIX: Original called .ToList() before .Where(), loading the full DiscountItems table into memory.
    //       Now filters server-side. Uses DateOnly-style date comparison via EF.Functions.
    [HttpGet("discount")]
    public async Task<ActionResult<DiscountItem>> GetDiscount([FromQuery] DateTime day)
    {
        var dayDate = day.Date;
        var discount = await _db.DiscountItems
            .Where(y => y.Start.Date <= dayDate && y.End.Date >= dayDate)
            .FirstOrDefaultAsync();

        if (discount is null)
            return NotFound();

        return Ok(discount);
    }

    // GET api/catalog/stock?date=2024-01-15&catalogItemId=1
    // Was: GetAvailableStock(DateTime date, int catalogItemId)
    // FIX: Original called .ToList() before second .Where(), loading all stock rows for the item into memory.
    //       Now filters both conditions server-side.
    [HttpGet("stock")]
    public async Task<ActionResult<int>> GetAvailableStock(
        [FromQuery] DateTime date,
        [FromQuery] int catalogItemId)
    {
        var targetDate = date.Date;
        var stock = await _db.CatalogItemsStocks
            .Where(x => x.CatalogItemId == catalogItemId && x.Date.Date == targetDate)
            .FirstOrDefaultAsync();

        return Ok(stock?.AvailableStock ?? 0);
    }

    // POST api/catalog/items
    // Was: CreateCatalogItem(CatalogItem catalogItem)
    // FIX: Original used Max(Id) + 1 for ID generation. Now relies on
    //       [DatabaseGenerated(DatabaseGeneratedOption.Identity)] on CatalogItem.Id,
    //       so EF Core lets the database assign the identity.
    [HttpPost("items")]
    public async Task<ActionResult<CatalogItem>> CreateCatalogItem([FromBody] CatalogItem catalogItem)
    {
        // Let the database assign the identity — do not set catalogItem.Id.
        catalogItem.Id = 0;
        _db.CatalogItems.Add(catalogItem);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(FindCatalogItem), new { id = catalogItem.Id }, catalogItem);
    }

    // PUT api/catalog/items
    // Was: UpdateCatalogItem(CatalogItem catalogItem)
    [HttpPut("items")]
    public async Task<IActionResult> UpdateCatalogItem([FromBody] CatalogItem catalogItem)
    {
        _db.Entry(catalogItem).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/catalog/items/{id}
    // Was: RemoveCatalogItem(CatalogItem catalogItem)
    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> RemoveCatalogItem(int id)
    {
        var item = await _db.CatalogItems.FindAsync(id);
        if (item is null)
            return NotFound();

        _db.CatalogItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/catalog/stock
    // Was: CreateAvailableStock(CatalogItemsStock catalogItemsStock)
    // FIX: Original used Max(StockId) + 1 for ID generation. Now relies on
    //       [DatabaseGenerated(DatabaseGeneratedOption.Identity)] on CatalogItemsStock.StockId.
    //       Also fixed in-memory filtering to server-side LINQ.
    [HttpPost("stock")]
    public async Task<IActionResult> CreateAvailableStock([FromBody] CatalogItemsStock catalogItemsStock)
    {
        var targetDate = catalogItemsStock.Date.Date;
        var existing = await _db.CatalogItemsStocks
            .Where(x => x.CatalogItemId == catalogItemsStock.CatalogItemId
                        && x.Date.Date == targetDate)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            // Overwrite the existing stock entry for that date.
            existing.AvailableStock = catalogItemsStock.AvailableStock;
            _db.Entry(existing).State = EntityState.Modified;
        }
        else
        {
            // Let the database assign the identity — do not set StockId.
            catalogItemsStock.StockId = 0;
            _db.CatalogItemsStocks.Add(catalogItemsStock);
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}
