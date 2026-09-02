namespace CorrosionDetection.Blazor.Models
{
    public class DetectionResponse
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public List<DetectionResult> Detections { get; set; } = new();
    }
}