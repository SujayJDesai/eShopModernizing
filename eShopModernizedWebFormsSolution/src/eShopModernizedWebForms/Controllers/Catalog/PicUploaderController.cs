using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace eShopModernizedWebForms.Controllers.Catalog;

/// <summary>
/// Handles catalog image uploads (migrated from PicUploader.asmx).
/// </summary>
[ApiController]
[Route("Catalog/PicUploader.asmx")]
public sealed class PicUploaderController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<PicUploaderController> _logger;

    public PicUploaderController(IImageService imageService, ILogger<PicUploaderController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }


    [HttpPost("UploadImage")]
    [Consumes("multipart/form-data")]
    public ActionResult UploadImage(
        [FromForm(Name = "HelpSectionImages")] IFormFile image,
        [FromForm(Name = "itemId")] string? itemId)
    {
        _logger.LogInformation("Now Processing... /Catalog/PicUploader.asmx");

        if (image is null || !IsValidImage(image))
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

        return Ok(tempImage);
    }

    private static bool IsValidImage(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[8];
            if (stream.Read(header) < 8)
                return false;

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                return true;

            // GIF: 47 49 46 38 (GIF8)
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                return true;

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
