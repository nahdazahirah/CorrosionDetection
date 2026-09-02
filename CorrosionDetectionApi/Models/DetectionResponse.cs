namespace CorrosionDetection.Models
{
    // Dibungkus bareng ukuran gambar asli, supaya frontend nggak perlu
    // menghitung ulang/menebak dimensi gambar sendiri - tinggal pakai
    // angka yang sama persis dengan yang dipakai backend waktu proses.
    public class DetectionResponse
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public List<DetectionResult> Detections { get; set; } = new();
    }
}