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

        // Relasi: 1 session bisa punya banyak item deteksi (banyak area korosi dalam 1 gambar)
        public List<DetectionItem> Items { get; set; } = new();
    }
}