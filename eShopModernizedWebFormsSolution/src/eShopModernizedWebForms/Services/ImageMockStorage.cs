using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using eShopModernizedWebForms.Models;
using System.IO;

namespace eShopModernizedWebForms.Services
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

            var pathPics = Path.Combine(_env.ContentRootPath, "Pics");
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