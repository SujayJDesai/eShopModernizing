using eShopLegacy.Utilities;
using eShopLegacyMVC.Services;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers.WebApi
{
    [ApiController]
    [Route("[controller]")]
    public class BrandsController :ControllerBase    {
        private ICatalogService _service;
        public BrandsController(ICatalogService service)
        {
            _service = service;
        }

        // GET api/<controller>
        public IEnumerable<Models.CatalogBrand> Get()
        {
            var brands = _service.GetCatalogBrands();
            return brands;
        }

        // GET api/<controller>/5
        public IActionResult Get(int id)
        {
            var brands = _service.GetCatalogBrands();
            var brand = brands.FirstOrDefault(x => x.Id == id);
            if (brand == null)
                return NotFound();
            return Ok(brand);
        }

        [HttpDelete]
        // DELETE api/<controller>/5
        public IActionResult Delete(int id)
        {
            var brandToDelete = _service.GetCatalogBrands().FirstOrDefault(x => x.Id == id);
            if (brandToDelete == null)
            {
                return NotFound();
            }

            // demo only - don't actually delete
            return Ok();
        }
    }
}