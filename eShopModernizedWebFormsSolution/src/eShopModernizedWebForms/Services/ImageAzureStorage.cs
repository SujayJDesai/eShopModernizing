using eShopModernizedWebForms.Models;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace eShopModernizedWebForms.Services
{
    public class ImageAzureStorage : IImageService
    {

        private readonly IAmazonS3 _s3Client;
        private readonly IWebHostEnvironment _env;
        // PORT-TODO
        // {"note": "S3 bucket name must be configured. CatalogConfiguration.S3BucketName must be added to CatalogConfiguration and populated from appsettings.json or environment variable (e.g. S3_BUCKET_NAME). Bucket names are globally unique in S3.", "files": ["CatalogConfiguration.cs", "appsettings.json"]}
        private readonly string _bucketName;
        private readonly string _s3BaseUrl;

        public ImageAzureStorage(IAmazonS3 s3Client, IWebHostEnvironment env, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _env = env;
            // PORT-TODO
            // {"note": "Replace CatalogConfiguration.S3BucketName with actual config property once it is added to CatalogConfiguration. Also register IAmazonS3 as a singleton in DI (Program.cs).", "files": ["CatalogConfiguration.cs", "Program.cs"]}
            _bucketName = configuration["S3BucketName"] ?? throw new InvalidOperationException("S3BucketName is not configured. Set it in appsettings.json or as an environment variable.");
            var regionName = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            _s3BaseUrl = $"https://{_bucketName}.s3.{regionName}.amazonaws.com/";
        }

        public string BaseUrl()
        {
            return _s3BaseUrl;
        }

        public string BuildUrlImage(CatalogItem item)
        {
            if (string.IsNullOrEmpty(item.PictureFileName))
                return UrlDefaultImage();

            return _s3BaseUrl + "pics/" + item.Id + "/" + item.PictureFileName;
        }

        public void Dispose()
        {
            // IAmazonS3 lifetime is managed by the DI container; do not dispose here.
        }

        public void InitializeCatalogImages()
        {
            // Ensure the S3 bucket exists (idempotent)
            try
            {
                _s3Client.PutBucketAsync(_bucketName).GetAwaiter().GetResult();
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou")
            {
                // Idempotent — bucket already exists and is owned by us.
            }

            // PORT-TODO
            // {"note": "Azure code set BlobContainerPublicAccessType.Blob for public read access. S3 equivalent requires setting a bucket policy for public read. This should be configured via infrastructure (e.g. CloudFormation/CDK) or a bucket policy, not in application code."}

            // Delete all existing objects under the 'pics/' prefix
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = "pics/"
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
                        Objects = listResponse.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                    };
                    _s3Client.DeleteObjectsAsync(deleteRequest).GetAwaiter().GetResult();
                }
                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            } while (listResponse.IsTruncated == true);

            var webRoot = Path.Combine(_env.ContentRootPath, "Pics");

            for (int i = 1; i <= 12; i++)
            {
                var path = Path.Combine(webRoot, i + ".png");
                var blobName = "pics/" + i + "/" + i + ".png";
                UpLoadImageFromFile(blobName, path, "image/png");
            }
            var defaultImagePath = Path.Combine(webRoot, "default.png");
            UpLoadImageFromFile("pics/temp/default.png", defaultImagePath, "image/png");
        }

        public void UpdateImage(CatalogItem item)
        {
            var folder = item.TempImageName.Replace("/pics/", string.Empty);
            var tempKey = "pics/" + folder;

            // Delete existing objects under item.Id prefix
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = "pics/" + item.Id + "/"
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
                        Objects = listResponse.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                    };
                    _s3Client.DeleteObjectsAsync(deleteRequest).GetAwaiter().GetResult();
                }
                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            } while (listResponse.IsTruncated == true);

            // Copy temp blob to the final location
            var fileName = Path.GetFileName(item.TempImageName);
            var destinationKey = "pics/" + item.Id + "/" + fileName;

            _s3Client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = _bucketName,
                SourceKey = tempKey,
                DestinationBucket = _bucketName,
                DestinationKey = destinationKey,
                MetadataDirective = S3MetadataDirective.COPY
            }).GetAwaiter().GetResult();

            // Delete the temp blob
            _s3Client.DeleteObjectAsync(_bucketName, tempKey).GetAwaiter().GetResult();
        }

        public string UploadTempImage(IFormFile file, int? catalogItemId)
        {
            string path = catalogItemId.HasValue ? catalogItemId + "/temp/" : "temp/" + Guid.NewGuid().ToString() + "/";
            var objectKey = "pics/" + path + file.FileName.ToLower();

            using var stream = file.OpenReadStream();
            stream.Seek(0, SeekOrigin.Begin);

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = file.ContentType,
                AutoCloseStream = false
            };
            _s3Client.PutObjectAsync(putRequest).GetAwaiter().GetResult();

            return _s3BaseUrl + objectKey;
        }

        public string UrlDefaultImage()
        {
            return _s3BaseUrl + "pics/temp/default.png";
        }

        private void UpLoadImageFromFile(string objectKey, string filePath, string contentType)
        {
            using var fileStream = File.OpenRead(filePath);
            fileStream.Seek(0, SeekOrigin.Begin);

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType,
                AutoCloseStream = false
            };
            _s3Client.PutObjectAsync(putRequest).GetAwaiter().GetResult();
        }


    }
}