using Microsoft.AspNetCore.Mvc;
using CorrosionDetection.Services;
using CorrosionDetection.Models;
using CorrosionDetectionApi.Data;

namespace CorrosionDetection.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorrosionController : ControllerBase
    {
        private readonly CorrosionDetectionService _detectionService;
        private readonly CorrosionDbContext _dbContext;

        public CorrosionController(CorrosionDetectionService detectionService, CorrosionDbContext dbContext)
        {
            _detectionService = detectionService;
            _dbContext = dbContext;
        }

        // POST api/corrosion/detect
        [HttpPost("detect")]
        public async Task<IActionResult> Detect(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Tidak ada gambar yang diupload.");

            using var stream = image.OpenReadStream();
            var response = _detectionService.DetectWithImageInfo(stream);

            // Simpan hasil deteksi ke database
            var session = new DetectionSession
            {
                SourceType = "upload",
                ImageWidth = response.ImageWidth,
                ImageHeight = response.ImageHeight,
                DetectionCount = response.Detections.Count,
                Items = response.Detections.Select(d => new DetectionItem
                {
                    X = d.X,
                    Y = d.Y,
                    Width = d.Width,
                    Height = d.Height,
                    Confidence = d.Confidence,
                    AreaPercentage = d.AreaPercentage,
                    MaskImageBase64 = d.MaskImageBase64
                }).ToList()
            };

            _dbContext.DetectionSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            return Ok(response);
        }

        // GET api/corrosion/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var sessions = await Task.FromResult(
                _dbContext.DetectionSessions
                    .OrderByDescending(s => s.Timestamp)
                    .Take(50)
                    .Select(s => new
                    {
                        s.Id,
                        s.Timestamp,
                        s.SourceType,
                        s.DetectionCount
                    })
                    .ToList()
            );

            return Ok(sessions);
        }
    }
}