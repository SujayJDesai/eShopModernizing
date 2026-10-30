using Amazon.S3;
using Amazon.S3.Model;
using eShopModernizedMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace eShopModernizedMVC.Services
{
    public class ImageAzureStorage : IImageService
    {

        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly IWebHostEnvironment _env;

        public ImageAzureStorage(IAmazonS3 s3Client, IWebHostEnvironment env)
        {
            _s3Client = s3Client;
            _bucketName = CatalogConfiguration.S3BucketName;
            _env = env;
        }

        public string BaseUrl()
        {
            var region = _s3Client.Config.RegionEndpoint.SystemName;
            return $"https://{_bucketName}.s3.{region}.amazonaws.com";
        }

        public string BuildUrlImage(CatalogItem item)
        {
            if (string.IsNullOrEmpty(item.PictureFileName))
                return UrlDefaultImage();

            var objectKey = item.Id + "/" + item.PictureFileName;
            return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(1)
            });
        }

        public void Dispose()
        {
        }

        public void InitializeCatalogImages()
        {
            // Create bucket idempotently (replaces container.CreateIfNotExists)
            try
            {
                _s3Client.PutBucketAsync(_bucketName).GetAwaiter().GetResult();
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou")
            {
                // Idempotent — bucket already exists in this account
            }


            // Delete all existing objects in the bucket
            var listRequest = new ListObjectsV2Request { BucketName = _bucketName };
            ListObjectsV2Response listResponse;
            do
            {
                listResponse = _s3Client.ListObjectsV2Async(listRequest).GetAwaiter().GetResult();
                if (listResponse.S3Objects != null && listResponse.S3Objects.Count > 0)
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = _bucketName,
                        Objects = listResponse.S3Objects
                            .Select(o => new KeyVersion { Key = o.Key })
                            .ToList()
                    };
                    _s3Client.DeleteObjectsAsync(deleteRequest).GetAwaiter().GetResult();
                }
                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            } while (listResponse.IsTruncated == true);

            // PORT-TODO
            // {"was": "System.Web.HttpContext.Current.Server.MapPath", "note": "Ideally inject IWebHostEnvironment and use WebRootPath; using AppDomain.CurrentDomain.BaseDirectory as interim replacement"}
            var webRoot = Path.Combine(_env.WebRootPath, "Pics");

            for (int i = 1; i <= 12; i++)
            {
                var path = Path.Combine(webRoot, i + ".png");
                var objectKey = i + "/" + i + ".png";
                UpLoadImageFromFile(objectKey, path, "image/png");

            }
            var defaultImagePath = Path.Combine(webRoot, "default.png");
            UpLoadImageFromFile("temp/default.png", defaultImagePath, "image/png");


        }

        public void UpdateImage(CatalogItem item)
        {
            var tempObjectKey = item.TempImageName.Replace("/pics/", string.Empty);

            // Delete existing objects under the item's prefix
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = item.Id + "/"
            };
            ListObjectsV2Response listResponse;
            do
            {
                listResponse = _s3Client.ListObjectsV2Async(listRequest).GetAwaiter().GetResult();
                if (listResponse.S3Objects != null && listResponse.S3Objects.Count > 0)
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = _bucketName,
                        Objects = listResponse.S3Objects
                            .Select(o => new KeyVersion { Key = o.Key })
                            .ToList()
                    };
                    _s3Client.DeleteObjectsAsync(deleteRequest).GetAwaiter().GetResult();
                }
                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            } while (listResponse.IsTruncated == true);

            // Copy temp object to final location
            var fileName = Path.GetFileName(item.TempImageName);
            var destinationKey = item.Id + "/" + fileName;

            _s3Client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = _bucketName,
                SourceKey = tempObjectKey,
                DestinationBucket = _bucketName,
                DestinationKey = destinationKey,
                MetadataDirective = S3MetadataDirective.COPY
            }).GetAwaiter().GetResult();

            // Delete the temp object (already idempotent)
            _s3Client.DeleteObjectAsync(_bucketName, tempObjectKey).GetAwaiter().GetResult();
        }

        public string UploadTempImage(IFormFile file, int? catalogItemId)
        {
            string objectKeyPrefix = catalogItemId.HasValue ? catalogItemId + "/temp/" : "temp/" + Guid.NewGuid().ToString() + "/";
            var objectKey = objectKeyPrefix + file.FileName.ToLower();

            var inputStream = file.OpenReadStream();
            inputStream.Seek(0, SeekOrigin.Begin);

            _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = inputStream,
                ContentType = file.ContentType,
                AutoCloseStream = true
            }).GetAwaiter().GetResult();

            return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(1)
            });
        }

        public string UrlDefaultImage()
        {
            return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = "temp/default.png",
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(1)
            });
        }

        private void UpLoadImageFromFile(string objectKey, string filePath, string contentType)
        {
            using var fileStream = File.OpenRead(filePath);
            fileStream.Seek(0, SeekOrigin.Begin);

            _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType,
                AutoCloseStream = false
            }).GetAwaiter().GetResult();
        }


    }
}