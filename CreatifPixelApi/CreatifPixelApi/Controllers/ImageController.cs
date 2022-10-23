using CreatifPixelLib.Interfaces;
using CreatifPixelLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace CreatifPixelApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly IImageProcessor _imageProcessor;
        private readonly ILicenseService _licenseService;
        private readonly IDocProcessor _docProcessor;
        private readonly ImageTransformConfig _options;

        public ImageController(ILogger<ImageController> logger,
            IImageProcessor imageProcessor,
            IDocProcessor docProcessor,
            ILicenseService licenseService,
            IOptions<ImageTransformConfig> options)
        {
            _logger = logger;
            _imageProcessor = imageProcessor;
            _licenseService = licenseService;
            _docProcessor = docProcessor;
            _options = options.Value;
        }

        [HttpPost("get-preview")]
        public ActionResult<BrickImagePreview> GetPreview([FromBody] BrickImage model)
        {
            if (model == null || model.Base64DataString == null) return null;

            var newImages = _imageProcessor.BuildNewImage(model.Base64DataString, PixelizedImageSizes.Medium, model.Contrast, - 1, false);

            if (!string.IsNullOrEmpty(newImages.errorCode)) return BadRequest(newImages.errorCode);

            return new BrickImagePreview
            {
                Base64StringImages = newImages.pixelizedImageSets.Select(x => x.Base64ImageString).ToArray(),
                Size = model.Size,
            };
        }

        [HttpPost("get-schema")]
        public ActionResult GetSchema2([FromBody] BrickImage model)
        {
            if (model == null || model.Base64DataString == null) return BadRequest();

            var license = _licenseService.GetLicenseByKey(model.LicenseKey);

            if (license == null) return BadRequest("NO_LICENSE_CODE");

            var newImages = _imageProcessor.BuildNewImage(model.Base64DataString, PixelizedImageSizes.Medium, model.Contrast, model.BuildByIndex, false);

            if (!string.IsNullOrEmpty(newImages.errorCode)) return BadRequest(newImages.errorCode);

            var bytes = _docProcessor.BuildPdfScheme(newImages.pixelizedImageSets[0].Pixels, newImages.name);

            return File(bytes, "application/octet-stream", "schema.pdf");
        }

        //[HttpPost("get-schema")]
        //public ActionResult GetSchema([FromBody] BrickImage model)
        //{
        //    if (model == null || model.Base64DataString == null) return BadRequest();

        //    var license = _licenseService.GetLicenseByKey(model.LicenseKey);

        //    if (license == null) return BadRequest("NO_LICENSE_CODE");

        //    //
        //    //var newImages = _imageProcessor.BuildNewImage(model.Base64DataString, license.Size, model.BuildByIndex, model.Contrast, false);

        //    //var bytes = _docProcessor.BuildPdfScheme(newImages.pixelizedImageSets[0].Pixels, newImages.name);
        //    //

        //    // FAKE PDF
        //    var fakePDFFileName = $"{_options.SchemaTemplateFolder}\\fake_schema.pdf";

        //    byte[] bytes;
        //    using (var fsSource = new FileStream(fakePDFFileName, FileMode.Open, FileAccess.Read))
        //    {
        //        bytes = new byte[fsSource.Length];
        //        fsSource.Read(bytes, 0, bytes.Length);
        //    }
        //    //

        //    return File(bytes, "application/octet-stream", "schema.pdf");
        //}

        [HttpPost("clean-up")]
        public ActionResult CleanUp([FromBody] BrickImage model)
        {
            var license = _licenseService.GetLicenseByKey(model.LicenseKey);

            if (license == null) return BadRequest();

            DirectoryInfo di = new DirectoryInfo(_options.OutputSchemaFolder);

            foreach (FileInfo file in di.GetFiles("*.zip"))
                file.Delete();

            return Ok();
        }

        public class NameResult
        {
            public string Name { get; set; }
        }
    }
}
