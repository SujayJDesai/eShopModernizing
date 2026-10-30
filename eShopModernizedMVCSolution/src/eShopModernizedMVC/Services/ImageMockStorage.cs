using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using eShopModernizedMVC.Models;
using System.IO;

namespace eShopModernizedMVC.Services
{
    public class ImageMockStorage : IImageService
    {
        private readonly IWebHostEnvironment _env;

        public ImageMockStorage(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string BaseUrl()
        {
            return GetBaseUrlImages();
        }

        public string BuildUrlImage(CatalogItem item)
        {
            var pictureFileName = string.IsNullOrEmpty(item.PictureFileName) ? "default.png" : item.PictureFileName;
            return GetBaseUrlImages() + pictureFileName;
        }

        public void Dispose()
        {

        }

        public void InitializeCatalogImages()
        {

        }

        public void UpdateImage(CatalogItem item)
        {

        }

        public string UploadTempImage(IFormFile file, int? catalogItemId)
        {
            if (!catalogItemId.HasValue)
                return UrlDefaultImage();

            // PORT-TODO
            // {"was": "System.Web.HttpContext.Current.Server.MapPath", "note": "Ideally inject IWebHostEnvironment and use WebRootPath; using AppDomain.CurrentDomain.BaseDirectory as interim replacement"}
            var pathPics = Path.Combine(_env.WebRootPath, "Pics");
            var imageExists = File.Exists(Path.Combine(pathPics, catalogItemId.Value + ".png"));

            if (imageExists)
                return BaseUrl() + catalogItemId.Value + ".png";


            return UrlDefaultImage();
        }

        public string UrlDefaultImage()
        {
            return GetBaseUrlImages() + "default.png";
        }

        private string GetBaseUrlImages()
        {
            return "/Pics/";
        }
    }
}