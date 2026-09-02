namespace CorrosionDetection.Models
{
    // Ini "kontrak data" antara backend dan frontend.
    // Backend isi objek ini, lalu dikirim sebagai JSON ke frontend.
    public class DetectionResult
    {
        public float X { get; set; }       // posisi kiri-atas kotak (pixel, relatif ke gambar asli)
        public float Y { get; set; }       // posisi kiri-atas kotak
        public float Width { get; set; }   // lebar kotak
        public float Height { get; set; }  // tinggi kotak
        public float Confidence { get; set; } // seberapa yakin model (0.0 - 1.0)
    }
}