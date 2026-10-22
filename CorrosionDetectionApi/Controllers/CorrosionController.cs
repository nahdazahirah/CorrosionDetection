using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CorrosionDetection.Services;
using CorrosionDetection.Models;
using CorrosionDetectionApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            [FromForm] string sourceType = "upload",
            [FromForm] string? unitName = null,
            [FromForm] string? area = null,
            [FromForm] string? equipmentTag = null,
            [FromForm] string? equipmentType = null,
            [FromForm] string? inspectionMethod = null,
            [FromForm] string? inspectorName = null,
            [FromForm] string? inspectorNip = null,
            [FromForm] string? notes = null)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Tidak ada gambar yang diupload.");

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            using var stream = new MemoryStream(imageBytes);
            var response = _detectionService.DetectWithImageInfo(stream);

            float maxArea = response.Detections.Any() ? response.Detections.Max(d => d.AreaPercentage) : 0f;
            float totalArea = response.Detections.Sum(d => d.AreaPercentage);

            var (severityGrade, recommendation) = EvaluatePertaminaCorrosionStandard(response.Detections.Count, maxArea, totalArea);

            var currentUser = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email);
            var finalInspectorName = !string.IsNullOrWhiteSpace(inspectorName) ? inspectorName : (currentUser ?? "Petugas Inspeksi Pertamina");
            var finalInspectorNip = !string.IsNullOrWhiteSpace(inspectorNip) ? inspectorNip : "NIP-REG-78241";

            var session = new DetectionSession
            {
                SourceType = sourceType,
                ImageWidth = response.ImageWidth,
                ImageHeight = response.ImageHeight,
                DetectionCount = response.Detections.Count,
                OriginalImageBase64 = Convert.ToBase64String(imageBytes),
                UnitName = string.IsNullOrWhiteSpace(unitName) ? "RU VI Balongan" : unitName,
                Area = string.IsNullOrWhiteSpace(area) ? "CDU-01 Process Area" : area,
                EquipmentTag = string.IsNullOrWhiteSpace(equipmentTag) ? "11-TK-102A" : equipmentTag,
                EquipmentType = string.IsNullOrWhiteSpace(equipmentType) ? "Process Piping (API 570)" : equipmentType,
                InspectionMethod = string.IsNullOrWhiteSpace(inspectionMethod) ? "Patroli Lapangan (Tablet)" : inspectionMethod,
                InspectorName = finalInspectorName,
                InspectorNip = finalInspectorNip,
                SeverityGrade = severityGrade,
                WorkOrderStatus = severityGrade == "Critical" ? "Perlu Work Order" : (severityGrade == "Severe" ? "Rekomendasi Pemeliharaan" : "Normal / Monitor"),
                WorkOrderNumber = severityGrade == "Critical" ? $"WO-EMERG-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}" : null,
                Notes = notes,
                Recommendation = recommendation,
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

            // Populate response with session data
            response.Id = session.Id;
            response.Timestamp = session.Timestamp;
            response.SourceType = session.SourceType;
            response.DetectionCount = session.DetectionCount;
            response.UnitName = session.UnitName;
            response.Area = session.Area;
            response.EquipmentTag = session.EquipmentTag;
            response.EquipmentType = session.EquipmentType;
            response.InspectionMethod = session.InspectionMethod;
            response.InspectorName = session.InspectorName;
            response.InspectorNip = session.InspectorNip;
            response.SeverityGrade = session.SeverityGrade;
            response.WorkOrderStatus = session.WorkOrderStatus;
            response.WorkOrderNumber = session.WorkOrderNumber;
            response.Notes = session.Notes;
            response.Recommendation = session.Recommendation;
            response.MaxAreaPercentage = maxArea;

            return Ok(response);
        }

        // GET api/corrosion/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
           [FromQuery] DateTime? fromDate,
           [FromQuery] DateTime? toDate,
           [FromQuery] string? sourceType,
           [FromQuery] string? unitName,
           [FromQuery] string? severityGrade,
           [FromQuery] string? search,
           [FromQuery] string sortBy = "date",
           [FromQuery] bool sortDesc = true)
        {
            var effectiveFrom = fromDate ?? DateTime.Today.AddDays(-30);
            var effectiveTo = toDate ?? DateTime.Today;

            var query = _dbContext.DetectionSessions
                .Include(s => s.Items)
                .Where(s => s.Timestamp >= effectiveFrom && s.Timestamp < effectiveTo.AddDays(1))
                .AsQueryable();

            if (!string.IsNullOrEmpty(sourceType))
                query = query.Where(s => s.SourceType == sourceType);

            if (!string.IsNullOrEmpty(unitName))
                query = query.Where(s => s.UnitName == unitName);

            if (!string.IsNullOrEmpty(severityGrade))
                query = query.Where(s => s.SeverityGrade == severityGrade);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s => 
                    (s.EquipmentTag != null && s.EquipmentTag.ToLower().Contains(term)) ||
                    (s.Area != null && s.Area.ToLower().Contains(term)) ||
                    (s.UnitName != null && s.UnitName.ToLower().Contains(term)) ||
                    (s.InspectorName != null && s.InspectorName.ToLower().Contains(term)));
            }

            query = sortBy switch
            {
                "area" => sortDesc
                    ? query.OrderByDescending(s => s.Items.Any() ? s.Items.Max(i => i.AreaPercentage) : 0)
                    : query.OrderBy(s => s.Items.Any() ? s.Items.Max(i => i.AreaPercentage) : 0),
                "count" => sortDesc
                    ? query.OrderByDescending(s => s.DetectionCount)
                    : query.OrderBy(s => s.DetectionCount),
                "severity" => sortDesc
                    ? query.OrderByDescending(s => s.SeverityGrade)
                    : query.OrderBy(s => s.SeverityGrade),
                _ => sortDesc
                    ? query.OrderByDescending(s => s.Timestamp)
                    : query.OrderBy(s => s.Timestamp)
            };

            var sessions = await query.Take(300).ToListAsync();

            var result = sessions.Select(s => new
            {
                s.Id,
                s.Timestamp,
                s.SourceType,
                s.ImageWidth,
                s.ImageHeight,
                s.DetectionCount,
                s.OriginalImageBase64,
                s.UnitName,
                s.Area,
                s.EquipmentTag,
                s.EquipmentType,
                s.InspectionMethod,
                s.InspectorName,
                s.InspectorNip,
                s.SeverityGrade,
                s.WorkOrderStatus,
                s.WorkOrderNumber,
                s.Notes,
                s.Recommendation,
                MaxAreaPercentage = s.Items.Any() ? s.Items.Max(i => i.AreaPercentage) : 0f,
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

        // GET api/corrosion/session/{id}
        [HttpGet("session/{id}")]
        public async Task<IActionResult> GetSessionById(int id)
        {
            var s = await _dbContext.DetectionSessions
                .Include(sess => sess.Items)
                .FirstOrDefaultAsync(sess => sess.Id == id);

            if (s == null)
                return NotFound("Sesi inspeksi tidak ditemukan.");

            var result = new
            {
                s.Id,
                s.Timestamp,
                s.SourceType,
                s.ImageWidth,
                s.ImageHeight,
                s.DetectionCount,
                s.OriginalImageBase64,
                s.UnitName,
                s.Area,
                s.EquipmentTag,
                s.EquipmentType,
                s.InspectionMethod,
                s.InspectorName,
                s.InspectorNip,
                s.SeverityGrade,
                s.WorkOrderStatus,
                s.WorkOrderNumber,
                s.Notes,
                s.Recommendation,
                MaxAreaPercentage = s.Items.Any() ? s.Items.Max(i => i.AreaPercentage) : 0f,
                TotalAreaPercentage = s.Items.Sum(i => i.AreaPercentage),
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
            };

            return Ok(result);
        }

        // PUT api/corrosion/session/{id}/work-order
        [HttpPut("session/{id}/work-order")]
        public async Task<IActionResult> UpdateWorkOrder(int id, [FromBody] WorkOrderUpdateRequest request)
        {
            var session = await _dbContext.DetectionSessions.FindAsync(id);
            if (session == null)
                return NotFound("Sesi inspeksi tidak ditemukan.");

            session.WorkOrderStatus = request.Status ?? session.WorkOrderStatus;
            if (!string.IsNullOrWhiteSpace(request.WorkOrderNumber))
                session.WorkOrderNumber = request.WorkOrderNumber;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                session.Notes = (session.Notes != null ? session.Notes + " | " : "") + request.Notes;

            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Work Order berhasil diperbarui.", session.WorkOrderStatus, session.WorkOrderNumber });
        }

        // GET api/corrosion/dashboard-stats
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] string? unitFilter)
        {
            var query = _dbContext.DetectionSessions.Include(s => s.Items).AsQueryable();
            if (!string.IsNullOrWhiteSpace(unitFilter) && unitFilter != "Semua")
            {
                query = query.Where(s => s.UnitName == unitFilter);
            }

            var totalSessions = await query.CountAsync();
            var allSessions = await query.OrderByDescending(s => s.Timestamp).Take(300).ToListAsync();

            int criticalCount = allSessions.Count(s => s.SeverityGrade == "Critical");
            int severeCount = allSessions.Count(s => s.SeverityGrade == "Severe");
            int moderateCount = allSessions.Count(s => s.SeverityGrade == "Moderate");
            int lowCount = allSessions.Count(s => s.SeverityGrade == "Low");
            int nominalCount = allSessions.Count(s => s.SeverityGrade == "Nominal" || s.DetectionCount == 0);

            var activeWorkOrders = allSessions.Count(s => s.WorkOrderStatus != null && (s.WorkOrderStatus.Contains("Work Order") || s.WorkOrderStatus.Contains("Pemeliharaan")));

            // Asset Health Index (0 - 100%)
            // Heavily penalizes critical and severe corrosion
            double healthScore = 100.0;
            if (totalSessions > 0)
            {
                double penalty = (criticalCount * 25.0 + severeCount * 12.0 + moderateCount * 4.0 + lowCount * 1.0) / totalSessions;
                healthScore = Math.Clamp(100.0 - penalty, 45.0, 99.8);
            }

            var recentAlerts = allSessions
                .Where(s => s.SeverityGrade == "Critical" || s.SeverityGrade == "Severe")
                .Take(6)
                .Select(s => new
                {
                    s.Id,
                    s.Timestamp,
                    s.EquipmentTag,
                    s.UnitName,
                    s.Area,
                    s.SeverityGrade,
                    s.InspectionMethod,
                    MaxAreaPercentage = s.Items.Any() ? s.Items.Max(i => i.AreaPercentage) : 0f,
                    s.WorkOrderStatus,
                    s.WorkOrderNumber
                });

            var unitStats = allSessions
                .GroupBy(s => s.UnitName ?? "Lainnya")
                .Select(g => new
                {
                    Unit = g.Key,
                    Count = g.Count(),
                    Critical = g.Count(x => x.SeverityGrade == "Critical"),
                    Severe = g.Count(x => x.SeverityGrade == "Severe")
                })
                .ToList();

            var equipmentStats = allSessions
                .GroupBy(s => s.EquipmentType ?? "Umum")
                .Select(g => new
                {
                    EquipmentType = g.Key,
                    Count = g.Count()
                })
                .ToList();

            return Ok(new
            {
                TotalInspections = totalSessions,
                HealthIndex = Math.Round(healthScore, 1),
                CriticalCount = criticalCount,
                SevereCount = severeCount,
                ModerateCount = moderateCount,
                LowCount = lowCount,
                NominalCount = nominalCount,
                ActiveWorkOrders = activeWorkOrders,
                RecentAlerts = recentAlerts,
                UnitStats = unitStats,
                EquipmentStats = equipmentStats
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("history/{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _dbContext.DetectionSessions.FindAsync(id);
            if (session == null)
                return NotFound();

            _dbContext.DetectionSessions.Remove(session);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        private static (string SeverityGrade, string Recommendation) EvaluatePertaminaCorrosionStandard(int count, float maxAreaPct, float totalAreaPct)
        {
            if (count == 0 || maxAreaPct < 0.1f)
            {
                return (
                    "Nominal",
                    "Kondisi integritas coating dan material nominal (aman). Lanjutkan jadwal inspeksi visual rutin 6 bulanan sesuai SOP Pemeliharaan Preventif Pertamina."
                );
            }

            if (maxAreaPct < 5.0f)
            {
                return (
                    "Low",
                    "Korosi permukaan ringan / noda oksidasi (ISO 12944 Ri 1). Belum ada indikasi pitting mendalam. Catat ke CML (Condition Monitoring Location) dan monitor pada siklus 3 bulan."
                );
            }

            if (maxAreaPct < 15.0f)
            {
                return (
                    "Moderate",
                    "Kerusakan coating primer/topcoat dan korosi atmosferik lokal (ISO 12944 Ri 2-3). Rekomendasi spot blasting (SSPC-SP 10 / Sa 2.5) dan touch-up protective coating pada jadwal preventive maintenance berikutnya."
                );
            }

            if (maxAreaPct < 30.0f)
            {
                return (
                    "Severe",
                    "Korosi aktif dengan pengelupasan kerak karat tebal (Scale Rust / Pitting Initiation). WAJIB lakukan verifikasi Ultrasonic Thickness (UT) Gauging dalam 14 hari untuk memastikan sisa ketebalan aman (Tmin API 570/653). Terbitkan Notifikasi Pemeliharaan SAP PM (M1)."
                );
            }

            return (
                "Critical",
                "BAHAYA INTEGRITAS STRUKTUR (CRITICAL HAZARD): Indikasi degradasi parah dengan risiko perforasi (kebocoran hidrokarbon) atau penipisan di bawah Tmin. Rekomendasi STOP WORK / isolasi parsial, pasang engineered temporary clamp, dan terbitkan Emergency Work Order (SAP PM Tipe M2) dalam 24 jam."
            );
        }
    }

    public class WorkOrderUpdateRequest
    {
        public string? Status { get; set; }
        public string? WorkOrderNumber { get; set; }
        public string? Notes { get; set; }
    }
}
