using eShopModernizedMVC.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace eShopModernizedMVC.Controllers
{
    public class PicController : Controller
    {
        private readonly ILogger<PicController> _logger;
        private static readonly ImageFormat[] ValidFormats =
        {
            ImageFormat.Jpeg,
            ImageFormat.Png,
            ImageFormat.Gif
        };
        private readonly IImageService _imageService;
        public PicController(ICatalogService service, IImageService imageService, ILogger<PicController> logger)
        {
            _imageService = imageService;
            _logger = logger;
        }

        [HttpPost]
        [Route("uploadimage")]
        public ActionResult UploadImage()
        {
            _logger.LogInformation("Now processing... /Pic/UploadImage");
            IFormFile image = Request.Form.Files["HelpSectionImages"];
            var itemId = Request.Form["itemId"];
            if (!IsValidImage(image))
            {
                return BadRequest("image is not valid");
            }

            int.TryParse(itemId, out var catalogItemId);
            var urlImageTemp = _imageService.UploadTempImage(image, catalogItemId);
            var tempImage = new
            {
                name = new Uri(urlImageTemp).PathAndQuery,
                url = urlImageTemp
            };
            return Json(tempImage);
        }

        private bool IsValidImage(IFormFile file)
        {
            bool isValidImage = true;
            try
            {
                using (var img = Image.FromStream(file.OpenReadStream()))
                {
                    isValidImage = ValidFormats.Contains(img.RawFormat);
                }
            }
            catch (Exception)
            {
                isValidImage = false;
            }

            return isValidImage;
        }
    }
}