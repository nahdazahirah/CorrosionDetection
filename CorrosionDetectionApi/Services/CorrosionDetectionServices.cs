using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using CorrosionDetection.Models;

namespace CorrosionDetection.Services
{
    // INI BACKEND-nya: semua logika AI ada di sini.
    // Frontend (Razor View + JS) sama sekali tidak menyentuh kode ini,
    // dia cuma memanggil lewat Controller.
    public class CorrosionDetectionService : IDisposable
    {
        private readonly InferenceSession _session;
        private const int InputSize = 640;           // HARUS SAMA dengan imgsz waktu training
        private const float ConfidenceThreshold = 0.25f; // ambang batas kepercayaan, bisa disesuaikan
        private const float IouThreshold = 0.45f;        // ambang batas buat NMS (hapus kotak duplikat)

        public CorrosionDetectionService(string modelPath)
        {
            // Load model ONNX sekali waktu aplikasi start (bukan tiap request),
            // supaya nggak lambat.
            _session = new InferenceSession(modelPath);
        }

        // Method baru: dipakai kalau frontend butuh tau ukuran gambar asli juga
        // (misal buat menghitung posisi kotak deteksi secara presisi di sisi Blazor)
        public DetectionResponse DetectWithImageInfo(Stream imageStream)
        {
            using var image = Image.Load<Rgb24>(imageStream);
            int origWidth = image.Width;
            int origHeight = image.Height;

            var detections = DetectFromLoadedImage(image, origWidth, origHeight);

            return new DetectionResponse
            {
                ImageWidth = origWidth,
                ImageHeight = origHeight,
                Detections = detections
            };
        }

        public List<DetectionResult> Detect(Stream imageStream)
        {
            using var image = Image.Load<Rgb24>(imageStream);
            int origWidth = image.Width;
            int origHeight = image.Height;
            return DetectFromLoadedImage(image, origWidth, origHeight);
        }

        private List<DetectionResult> DetectFromLoadedImage(Image<Rgb24> image, int origWidth, int origHeight)
        {

            // Resize ke ukuran yang diharapkan model.
            // CATATAN: ini stretch resize (simpel), bukan letterbox.
            // Kalau nanti mau presisi lebih tinggi, bisa diganti pakai
            // teknik letterbox (resize + padding) seperti yang dipakai Ultralytics.
            using var resized = image.Clone(ctx => ctx.Resize(InputSize, InputSize));

            // Susun jadi tensor [1, 3, 640, 640], nilai pixel dinormalisasi 0-1
            var input = new DenseTensor<float>(new[] { 1, 3, InputSize, InputSize });
            for (int y = 0; y < InputSize; y++)
            {
                for (int x = 0; x < InputSize; x++)
                {
                    var pixel = resized[x, y];
                    input[0, 0, y, x] = pixel.R / 255f;
                    input[0, 1, y, x] = pixel.G / 255f;
                    input[0, 2, y, x] = pixel.B / 255f;
                }
            }

            var inputName = _session.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, input) };

            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();
            // Untuk model 1 kelas, shape output biasanya [1, 5, 8400]
            // 5 = (center_x, center_y, width, height, confidence_score)
            // 8400 = jumlah "anchor point" yang dicek model di seluruh gambar

            var candidates = new List<DetectionResult>();
            int numAnchors = output.Dimensions[2];

            float scaleX = origWidth / (float)InputSize;
            float scaleY = origHeight / (float)InputSize;

            for (int i = 0; i < numAnchors; i++)
            {
                float score = output[0, 4, i];
                if (score < ConfidenceThreshold) continue;

                float cx = output[0, 0, i];
                float cy = output[0, 1, i];
                float w = output[0, 2, i];
                float h = output[0, 3, i];

                // Ubah dari (center, width, height) ke (top-left, width, height),
                // sekalian skalakan balik ke ukuran gambar asli.
                float x = (cx - w / 2f) * scaleX;
                float y = (cy - h / 2f) * scaleY;
                float boxW = w * scaleX;
                float boxH = h * scaleY;

                candidates.Add(new DetectionResult
                {
                    X = x,
                    Y = y,
                    Width = boxW,
                    Height = boxH,
                    Confidence = score
                });
            }

            // Model biasanya kasih banyak kotak tumpang tindih untuk 1 objek yang sama.
            // NMS (Non-Max Suppression) buang kotak yang saling overlap, sisain yang paling yakin.
            return NonMaxSuppression(candidates, IouThreshold);
        }

        private List<DetectionResult> NonMaxSuppression(List<DetectionResult> boxes, float iouThreshold)
        {
            var sorted = boxes.OrderByDescending(b => b.Confidence).ToList();
            var selected = new List<DetectionResult>();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                selected.Add(best);
                sorted.RemoveAt(0);

                // Buang semua kotak lain yang terlalu overlap dengan 'best'
                sorted.RemoveAll(b => IoU(best, b) > iouThreshold);
            }

            return selected;
        }

        // IoU (Intersection over Union) = seberapa besar dua kotak saling tumpang tindih
        private float IoU(DetectionResult a, DetectionResult b)
        {
            float x1 = Math.Max(a.X, b.X);
            float y1 = Math.Max(a.Y, b.Y);
            float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            float interArea = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            float unionArea = (a.Width * a.Height) + (b.Width * b.Height) - interArea;

            return unionArea <= 0 ? 0 : interArea / unionArea;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}