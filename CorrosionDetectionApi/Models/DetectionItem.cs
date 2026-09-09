namespace CorrosionDetection.Models
{
    public class DetectionItem
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public DetectionSession DetectionSession { get; set; } = null!;

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Confidence { get; set; }
        public float AreaPercentage { get; set; }
        public string? MaskImageBase64 { get; set; }
    }
}