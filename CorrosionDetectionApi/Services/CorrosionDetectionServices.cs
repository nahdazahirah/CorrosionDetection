using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using CorrosionDetection.Models;

namespace CorrosionDetection.Services
{
    public class CorrosionDetectionService : IDisposable
    {
        private readonly InferenceSession _session;
        private const int InputSize = 640;               // HARUS SAMA dengan imgsz waktu training
        private const float ConfidenceThreshold = 0.25f; // ambang batas kepercayaan
        private const float IouThreshold = 0.45f;        // ambang batas buat NMS
        private const float MaskThreshold = 0.5f;        // ambang batas biner buat mask (setelah sigmoid)

        // Model YOLOv8n-seg 1 kelas: output0 = [1, 37, 8400] -> 4 bbox + 1 class score + 32 mask coeff
        private const int NumMaskCoeffs = 32;
        private const int ProtoSize = 160;

        public CorrosionDetectionService(string modelPath)
        {
            _session = new InferenceSession(modelPath);
        }

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
            // Resize ke ukuran yang diharapkan model (stretch resize, sama seperti sebelumnya).
            using var resized = image.Clone(ctx => ctx.Resize(InputSize, InputSize));

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

            // PENTING: model sekarang punya 2 output, harus diambil BY NAME.
            var output0 = results.First(r => r.Name == "output0").AsTensor<float>(); // [1, 37, 8400]
            var output1 = results.First(r => r.Name == "output1").AsTensor<float>(); // [1, 32, 160, 160]

            int numAnchors = output0.Dimensions[2]; // 8400
            float scaleX = origWidth / (float)InputSize;
            float scaleY = origHeight / (float)InputSize;

            var candidates = new List<(DetectionResult box, float[] coeffs)>();

            for (int i = 0; i < numAnchors; i++)
            {
                float score = output0[0, 4, i]; // index 4 = class score (cuma 1 kelas)
                if (score < ConfidenceThreshold) continue;

                float cx = output0[0, 0, i];
                float cy = output0[0, 1, i];
                float w = output0[0, 2, i];
                float h = output0[0, 3, i];

                float x = (cx - w / 2f) * scaleX;
                float y = (cy - h / 2f) * scaleY;
                float boxW = w * scaleX;
                float boxH = h * scaleY;

                // Ambil 32 mask coefficient (index 5 sampai 36)
                var coeffs = new float[NumMaskCoeffs];
                for (int c = 0; c < NumMaskCoeffs; c++)
                    coeffs[c] = output0[0, 5 + c, i];

                candidates.Add((new DetectionResult
                {
                    X = x,
                    Y = y,
                    Width = boxW,
                    Height = boxH,
                    Confidence = score
                }, coeffs));
            }

            var kept = NonMaxSuppression(candidates, IouThreshold);

            // Untuk tiap deteksi yang lolos NMS, rekonstruksi mask-nya, hitung luas area,
            // lalu encode mask jadi gambar PNG base64 supaya gampang ditampilkan di Blazor.
            foreach (var (box, coeffs) in kept)
            {
                var (maskPixels, areaPercentage) = DecodeMask(
                    coeffs, output1, box, origWidth, origHeight, InputSize);

                int w = Math.Max(1, (int)box.Width);
                int h = Math.Max(1, (int)box.Height);

                box.MaskImageBase64 = EncodeMaskAsPngBase64(maskPixels, w, h);
                box.AreaPercentage = areaPercentage;
            }

            return kept.Select(k => k.box).ToList();
        }

        // Rekonstruksi mask biner dari mask coefficients + prototype masks,
        // lalu crop ke area bbox dan resize ke ukuran gambar asli.
        private (bool[] maskPixels, float areaPercentage) DecodeMask(
            float[] coeffs, Tensor<float> protos, DetectionResult box,
            int origWidth, int origHeight, int inputSize)
        {
            // 1. Matrix multiply: 32 coeffs x [32, 160, 160] -> mask mentah 160x160
            var rawMask = new float[ProtoSize, ProtoSize];
            for (int py = 0; py < ProtoSize; py++)
            {
                for (int px = 0; px < ProtoSize; px++)
                {
                    float sum = 0f;
                    for (int c = 0; c < NumMaskCoeffs; c++)
                        sum += coeffs[c] * protos[0, c, py, px];
                    rawMask[py, px] = Sigmoid(sum);
                }
            }

            // 2. Prototype 160x160 itu representasi dari input 640x640 (skala 1/4).
            //    Bbox kita sekarang dalam skala origWidth/origHeight, jadi balikkan dulu
            //    ke skala input (640) lalu ke skala proto (160) supaya bisa crop yang tepat.
            float scaleXOrigToInput = inputSize / (float)origWidth;
            float scaleYOrigToInput = inputSize / (float)origHeight;
            float protoScale = ProtoSize / (float)inputSize; // 160/640 = 0.25

            int px0 = (int)(box.X * scaleXOrigToInput * protoScale);
            int py0 = (int)(box.Y * scaleYOrigToInput * protoScale);
            int px1 = (int)((box.X + box.Width) * scaleXOrigToInput * protoScale);
            int py1 = (int)((box.Y + box.Height) * scaleYOrigToInput * protoScale);

            px0 = Math.Clamp(px0, 0, ProtoSize - 1);
            py0 = Math.Clamp(py0, 0, ProtoSize - 1);
            px1 = Math.Clamp(px1, px0 + 1, ProtoSize);
            py1 = Math.Clamp(py1, py0 + 1, ProtoSize);

            int cropW = px1 - px0;
            int cropH = py1 - py0;

            // 3. Resize crop itu ke ukuran bbox asli (nearest-neighbour, cukup buat kebutuhan ini)
            int outW = Math.Max(1, (int)box.Width);
            int outH = Math.Max(1, (int)box.Height);
            var maskPixels = new bool[outW * outH];
            int onCount = 0;

            for (int y = 0; y < outH; y++)
            {
                int srcY = py0 + (int)(y / (float)outH * cropH);
                srcY = Math.Clamp(srcY, 0, ProtoSize - 1);
                for (int x = 0; x < outW; x++)
                {
                    int srcX = px0 + (int)(x / (float)outW * cropW);
                    srcX = Math.Clamp(srcX, 0, ProtoSize - 1);

                    bool isMask = rawMask[srcY, srcX] > MaskThreshold;
                    maskPixels[y * outW + x] = isMask;
                    if (isMask) onCount++;
                }
            }

            float areaPercentage = outW * outH == 0 ? 0f : (onCount / (float)(outW * outH)) * 100f;
            return (maskPixels, areaPercentage);
        }

        // Ubah array boolean mask jadi gambar PNG (base64) berwarna merah semi-transparan,
        // supaya bisa langsung ditaruh sebagai <img> overlay di Blazor tanpa perlu canvas/JS.
        private string EncodeMaskAsPngBase64(bool[] maskPixels, int width, int height)
        {
            using var maskImage = new Image<Rgba32>(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isMask = maskPixels[y * width + x];
                    maskImage[x, y] = isMask
                        ? new Rgba32(255, 0, 0, 120)   // merah semi-transparan = area korosi
                        : new Rgba32(0, 0, 0, 0);       // transparan penuh = bukan korosi
                }
            }

            using var ms = new MemoryStream();
            maskImage.Save(ms, new PngEncoder());
            return Convert.ToBase64String(ms.ToArray());
        }

        private float Sigmoid(float x) => 1f / (1f + MathF.Exp(-x));

        private List<(DetectionResult box, float[] coeffs)> NonMaxSuppression(
            List<(DetectionResult box, float[] coeffs)> boxes, float iouThreshold)
        {
            var sorted = boxes.OrderByDescending(b => b.box.Confidence).ToList();
            var selected = new List<(DetectionResult, float[])>();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                selected.Add(best);
                sorted.RemoveAt(0);
                sorted.RemoveAll(b => IoU(best.box, b.box) > iouThreshold);
            }

            return selected;
        }

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