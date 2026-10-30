using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using eShopWCFService.Models;

namespace eShopWCFService
{

    /// <summary>
    /// HTTP client for CatalogService Web API.
    /// Formerly a WCF ClientBase&lt;ICatalogService&gt; proxy; now uses HttpClient
    /// to call the REST endpoints exposed by CatalogController.
    /// </summary>
    public class CatalogServiceClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public CatalogServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public CatalogItem FindCatalogItem(int id)
        {
            var response = _httpClient.GetFromJsonAsync<CatalogItem>($"api/catalog/items/{id}").GetAwaiter().GetResult();
            return response;
        }

        public List<CatalogBrand> GetCatalogBrands()
        {
            return _httpClient.GetFromJsonAsync<List<CatalogBrand>>("api/catalog/brands").GetAwaiter().GetResult()
                   ?? new List<CatalogBrand>();
        }

        public List<CatalogItem> GetCatalogItems(int brandIdFilter = 0, int typeIdFilter = 0)
        {
            return _httpClient.GetFromJsonAsync<List<CatalogItem>>(
                       $"api/catalog/items?brandIdFilter={brandIdFilter}&typeIdFilter={typeIdFilter}")
                       .GetAwaiter().GetResult()
                   ?? new List<CatalogItem>();
        }

        public List<CatalogType> GetCatalogTypes()
        {
            return _httpClient.GetFromJsonAsync<List<CatalogType>>("api/catalog/types").GetAwaiter().GetResult()
                   ?? new List<CatalogType>();
        }

        public int GetAvailableStock(DateTime date, int catalogItemId)
        {
            return _httpClient.GetFromJsonAsync<int>(
                $"api/catalog/stock?date={date:yyyy-MM-dd}&catalogItemId={catalogItemId}").GetAwaiter().GetResult();
        }

        public void CreateAvailableStock(CatalogItemsStock catalogItemsStock)
        {
            _httpClient.PostAsJsonAsync("api/catalog/stock", catalogItemsStock).GetAwaiter().GetResult();
        }

        public void CreateCatalogItem(CatalogItem catalogItem)
        {
            _httpClient.PostAsJsonAsync("api/catalog/items", catalogItem).GetAwaiter().GetResult();
        }

        public void UpdateCatalogItem(CatalogItem catalogItem)
        {
            _httpClient.PutAsJsonAsync("api/catalog/items", catalogItem).GetAwaiter().GetResult();
        }

        public void RemoveCatalogItem(CatalogItem catalogItem)
        {
            _httpClient.DeleteAsync($"api/catalog/items/{catalogItem.Id}").GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            // HttpClient lifetime is managed by IHttpClientFactory; do not dispose it here.
        }
    }
}
