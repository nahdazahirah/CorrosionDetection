namespace CorrosionDetection.Blazor.Models
{
    // Struktur ini HARUS PERSIS SAMA dengan DetectionResult di backend,
    // karena ini yang dipakai buat "membaca" JSON yang dikirim balik dari API.
    public class DetectionResult
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Confidence { get; set; }
        public string? MaskImageBase64 { get; set; }
        public float AreaPercentage { get; set; }
    }
}