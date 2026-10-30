using eShopLegacy.Utilities;
using eShopLegacyMVC.Services;
using System;
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
    public class FilesController :ControllerBase    {
        private ICatalogService _service;
        public FilesController(ICatalogService service)
        {
            _service = service;
        }

        // GET api/<controller>
        public IActionResult Get()
        {
            var brands = _service.GetCatalogBrands().Select(b => new BrandDTO { Id = b.Id, Brand = b.Brand }).ToList();
            var serializer = new Serializing();
            var stream = serializer.SerializeBinary(brands);
            return File(stream, "application/octet-stream");
        }

        [Serializable]
        public class BrandDTO
        {
            public int Id { get; set; }
            public string Brand { get; set; }
        }
    }
}