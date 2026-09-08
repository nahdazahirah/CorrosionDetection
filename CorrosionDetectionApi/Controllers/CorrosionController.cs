using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CorrosionDetection.Services;
using CorrosionDetection.Models;
using CorrosionDetectionApi.Data;
using Microsoft.EntityFrameworkCore;


namespace CorrosionDetection.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        public async Task<IActionResult> Detect(
            IFormFile image,
            [FromQuery] string sourceType = "upload")
        {
            if (image == null || image.Length == 0)
                return BadRequest("Tidak ada gambar yang diupload.");

            using var stream = image.OpenReadStream();
            var response = _detectionService.DetectWithImageInfo(stream);

            // Simpan hasil deteksi ke database
            var session = new DetectionSession
            {
                SourceType = sourceType == "webcam" ? "webcam" : "upload",
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
        public async Task<IActionResult> GetHistory(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? sourceType,
            [FromQuery] string sortBy = "date",
            [FromQuery] bool sortDesc = true)
        {
            var query = _dbContext.DetectionSessions
                .Include(s => s.Items)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(s => s.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.Timestamp <= toDate.Value.AddDays(1));

            if (!string.IsNullOrEmpty(sourceType))
                query = query.Where(s => s.SourceType == sourceType);

            // Sort
            query = sortBy switch
            {
                "area" => sortDesc
                    ? query.OrderByDescending(s => s.Items.Any() ? s.Items.Max(i => i.AreaPercentage) : 0)
                    : query.OrderBy(s => s.Items.Any() ? s.Items.Max(i => i.AreaPercentage) : 0),
                "count" => sortDesc
                    ? query.OrderByDescending(s => s.DetectionCount)
                    : query.OrderBy(s => s.DetectionCount),
                _ => sortDesc
                    ? query.OrderByDescending(s => s.Timestamp)
                    : query.OrderBy(s => s.Timestamp)
            };

            var sessions = await query.Take(200).ToListAsync();

            var result = sessions.Select(s => new
            {
                s.Id,
                s.Timestamp,
                s.SourceType,
                s.ImageWidth,
                s.ImageHeight,
                s.DetectionCount,
                MaxAreaPercentage = s.Items.Any() ? s.Items.Max(i => i.AreaPercentage) : 0,
                Items = s.Items.Select(i => new
                {
                    i.X,
                    i.Y,
                    i.Width,
                    i.Height,
                    i.Confidence,
                    i.AreaPercentage,
                    i.MaskImageBase64
                })
            });

            return Ok(result);
        }
    }
}