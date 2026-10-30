using eShopPorted.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Http;


namespace eShopPorted.Controllers
{
    public class PicController : Controller
    {
        private readonly ILogger<PicController> _logger;

        public const string GetPicRouteName = "GetPicRouteTemplate";

        private ICatalogService service;

        public PicController(ICatalogService service, ILogger<PicController> logger)
        {
            this.service = service;
            _logger = logger;
        }

        // GET: Pic/5.png
        [HttpGet]
        [Route("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
        public ActionResult Index(int catalogItemId)
        {
            _logger.LogInformation($"Now loading... /items/Index?{catalogItemId}/pic");

            if (catalogItemId <= 0)
            {
                return new StatusCodeResult(StatusCodes.Status400BadRequest);
            }

            var item = service.FindCatalogItem(catalogItemId);

            if (item != null)
            {
                var webRoot = "";
                //var webRoot = Server.MapPath("~/Pics");
                var path = Path.Combine(webRoot, item.PictureFileName);

                string imageFileExtension = Path.GetExtension(item.PictureFileName);
                string mimetype = GetImageMimeTypeFromImageFileExtension(imageFileExtension);

                var buffer = System.IO.File.ReadAllBytes(path);

                return File(buffer, mimetype);
            }

            return NotFound();
        }

        private string GetImageMimeTypeFromImageFileExtension(string extension)
        {
            string mimetype;

            switch (extension)
            {
                case ".png":
                    mimetype = "image/png";
                    break;
                case ".gif":
                    mimetype = "image/gif";
                    break;
                case ".jpg":
                case ".jpeg":
                    mimetype = "image/jpeg";
                    break;
                case ".bmp":
                    mimetype = "image/bmp";
                    break;
                case ".tiff":
                    mimetype = "image/tiff";
                    break;
                case ".wmf":
                    mimetype = "image/wmf";
                    break;
                case ".jp2":
                    mimetype = "image/jp2";
                    break;
                case ".svg":
                    mimetype = "image/svg+xml";
                    break;
                default:
                    mimetype = "application/octet-stream";
                    break;
            }

            return mimetype;
        }
    }
}
