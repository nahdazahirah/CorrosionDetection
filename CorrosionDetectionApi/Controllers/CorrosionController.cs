using Microsoft.AspNetCore.Mvc;
using CorrosionDetection.Services;

namespace CorrosionDetection.Api.Controllers
{
    // Ini BACKEND murni Web API - tidak ada View sama sekali.
    // [ApiController] + [Route] artinya semua action di sini otomatis
    // jadi endpoint JSON, bukan halaman HTML.
    [ApiController]
    [Route("api/[controller]")]
    public class CorrosionController : ControllerBase
    {
        private readonly CorrosionDetectionService _detectionService;

        public CorrosionController(CorrosionDetectionService detectionService)
        {
            _detectionService = detectionService;
        }

        // POST api/corrosion/detect
        [HttpPost("detect")]
        public IActionResult Detect(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Tidak ada gambar yang diupload.");

            using var stream = image.OpenReadStream();
            var response = _detectionService.DetectWithImageInfo(stream);

            return Ok(response);
        }
    }
}