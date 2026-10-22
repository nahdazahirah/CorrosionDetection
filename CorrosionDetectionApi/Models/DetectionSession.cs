using CorrosionDetection.Models;

namespace CorrosionDetection.Models
{
    public class DetectionSession
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SourceType { get; set; } = "upload";
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public int DetectionCount { get; set; }

        // Gambar asli
        public string? OriginalImageBase64 { get; set; }

        // Metadata Enterprise Pertamina
        public string? UnitName { get; set; }
        public string? Area { get; set; }
        public string? EquipmentTag { get; set; }
        public string? EquipmentType { get; set; }
        public string? InspectionMethod { get; set; }
        public string? InspectorName { get; set; }
        public string? InspectorNip { get; set; }
        public string? SeverityGrade { get; set; }
        public string? WorkOrderStatus { get; set; }
        public string? WorkOrderNumber { get; set; }
        public string? Notes { get; set; }
        public string? Recommendation { get; set; }

        // Relasi: 1 session bisa punya banyak item deteksi
        public List<DetectionItem> Items { get; set; } = new();
    }
}
