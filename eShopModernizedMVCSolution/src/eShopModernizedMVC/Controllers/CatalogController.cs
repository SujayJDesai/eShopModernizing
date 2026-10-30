using System.Collections.Generic;
using System.Net;
using eShopModernizedMVC.Models;
using eShopModernizedMVC.Services;
using System.IO;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc.Rendering;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace eShopModernizedMVC.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ILogger<CatalogController> _logger;
        private readonly ICatalogService _service;
        private readonly IImageService _imageService;
        public CatalogController(ICatalogService service, IImageService imageService, ILogger<CatalogController> logger)
        {
            _service = service;
            _imageService = imageService;
            _logger = logger;
        }

        // GET /[?pageSize=3&pageIndex=10]
        public ActionResult Index(int pageSize = 10, int pageIndex = 0)
        {
            _logger.LogInformation("Now loading... /Catalog/Index?pageSize={PageSize}&pageIndex={PageIndex}", pageSize, pageIndex);
            var paginatedItems = _service.GetCatalogItemsPaginated(pageSize, pageIndex);
            ChangeUriPlaceholder(paginatedItems.Data);
            return View(paginatedItems);
        }

        // GET: Catalog/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new StatusCodeResult(StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Now loading... /Catalog/Details?id={Id}", id);
            CatalogItem catalogItem = _service.FindCatalogItem(id.Value);
            if (catalogItem == null)
            {
                return NotFound();
            }

            AddUriPlaceHolder(catalogItem);
            return View(catalogItem);
        }

        // GET: Catalog/Create
        [Authorize]
        public ActionResult Create()
        {
            _logger.LogInformation("Now loading... /Catalog/Create");
            ViewBag.CatalogBrandId = new SelectList(_service.GetCatalogBrands(), "Id", "Brand");
            ViewBag.CatalogTypeId = new SelectList(_service.GetCatalogTypes(), "Id", "Type");
            ViewBag.UseAzureStorage = CatalogConfiguration.UseAzureStorage;
            return View(new CatalogItem() { PictureUri = _imageService.UrlDefaultImage() });
        }

        // POST: Catalog/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        public ActionResult Create([Bind("Id,Name,Description,Price,PictureFileName,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder,TempImageName")] CatalogItem catalogItem)
        {
            _logger.LogInformation("Now processing... /Catalog/Create?catalogItemName={CatalogItemName}", catalogItem.Name);
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(catalogItem.TempImageName))
                {
                    var fileName = Path.GetFileName(catalogItem.TempImageName);
                    catalogItem.PictureFileName = fileName;
                }

                _service.CreateCatalogItem(catalogItem);
                if (!string.IsNullOrEmpty(catalogItem.TempImageName))
                {
                    _imageService.UpdateImage(catalogItem);
                }

                return RedirectToAction("Index");
            }

            ViewBag.CatalogBrandId = new SelectList(_service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new SelectList(_service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            ViewBag.UseAzureStorage = CatalogConfiguration.UseAzureStorage;
            return View(catalogItem);
        }

        // GET: Catalog/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new StatusCodeResult(StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Now loading... /Catalog/Edit?id={Id}", id);
            CatalogItem catalogItem = _service.FindCatalogItem(id.Value);
            if (catalogItem == null)
            {
                return NotFound();
            }

            AddUriPlaceHolder(catalogItem);
            ViewBag.CatalogBrandId = new SelectList(_service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new SelectList(_service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            ViewBag.UseAzureStorage = CatalogConfiguration.UseAzureStorage;
            return View(catalogItem);
        }

        // POST: Catalog/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        public ActionResult Edit([Bind("Id,Name,Description,Price,PictureFileName,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder,TempImageName")] CatalogItem catalogItem)
        {
            _logger.LogInformation("Now processing... /Catalog/Edit?id={Id}", catalogItem.Id);
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(catalogItem.TempImageName))
                {
                    _imageService.UpdateImage(catalogItem);
                    var fileName = Path.GetFileName(catalogItem.TempImageName);
                    catalogItem.PictureFileName = fileName;
                }

                _service.UpdateCatalogItem(catalogItem);
                return RedirectToAction("Index");
            }

            ViewBag.CatalogBrandId = new SelectList(_service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new SelectList(_service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            ViewBag.UseAzureStorage = CatalogConfiguration.UseAzureStorage;
            return View(catalogItem);
        }

        // GET: Catalog/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            _logger.LogInformation("Now loading... /Catalog/Delete?id={Id}", id);
            if (id == null)
            {
                return new StatusCodeResult(StatusCodes.Status400BadRequest);
            }

            CatalogItem catalogItem = _service.FindCatalogItem(id.Value);
            if (catalogItem == null)
            {
                return NotFound();
            }

            AddUriPlaceHolder(catalogItem);
            return View(catalogItem);
        }

        // POST: Catalog/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize]
        public ActionResult DeleteConfirmed(int id)
        {
            _logger.LogInformation("Now processing... /Catalog/DeleteConfirmed?id={Id}", id);
            CatalogItem catalogItem = _service.FindCatalogItem(id);
            _service.RemoveCatalogItem(catalogItem);
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _service.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ChangeUriPlaceholder(IEnumerable<CatalogItem> items)
        {
            foreach (var catalogItem in items)
            {
                AddUriPlaceHolder(catalogItem);
            }
        }

        private void AddUriPlaceHolder(CatalogItem item)
        {
            item.PictureUri = _imageService.BuildUrlImage(item);
        }
    }
}