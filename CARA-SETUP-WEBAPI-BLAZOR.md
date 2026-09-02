# Setup: Backend (Web API) + Frontend (Blazor) — 2 Project Terpisah

## Struktur

```
CorrosionDetectionApi/       <- BACKEND, project "ASP.NET Core Web API"
  Controllers/CorrosionController.cs
  Models/DetectionResponse.cs
  (dan Models/DetectionResult.cs, Services/CorrosionDetectionService.cs
   dari yang sudah dibuat sebelumnya - copy ke sini)
  Program.cs   <- lihat Program-snippet.cs untuk bagian yang perlu ditambah
  Models/AI/best.onnx

CorrosionDetectionBlazor/    <- FRONTEND, project "Blazor WebAssembly Standalone App"
  Pages/Corrosion.razor
  Models/DetectionResult.cs
  Models/DetectionResponse.cs
  Program.cs   <- perlu daftarkan HttpClient (lihat langkah 4)
```

## Langkah setup

### 1. Buat 2 project di Visual Studio (dalam 1 Solution)
- File > New > Project > "ASP.NET Core Web API" — beri nama `CorrosionDetectionApi`
- Klik kanan Solution > Add > New Project > "Blazor WebAssembly Standalone App"
  (atau "Blazor WebAssembly App" tergantung versi VS kamu) — beri nama
  `CorrosionDetectionBlazor`

### 2. Install NuGet packages di project API
```
Install-Package Microsoft.ML.OnnxRuntime
Install-Package SixLabors.ImageSharp
```

### 3. Setup backend (CorrosionDetectionApi)
- Copy `Controllers/CorrosionController.cs`, `Models/DetectionResult.cs`,
  `Models/DetectionResponse.cs`, `Services/CorrosionDetectionService.cs` ke project ini
- Taruh `best.onnx` di `Models/AI/`
- Edit `Program.cs` sesuai `Program-snippet.cs` (termasuk bagian CORS!)
- Cek port yang dipakai project ini di `Properties/launchSettings.json`
  (misal `https://localhost:7050`) — catat, dipakai di langkah 4

### 4. Setup frontend (CorrosionDetectionBlazor)
- Copy `Pages/Corrosion.razor`, `Models/DetectionResult.cs`, `Models/DetectionResponse.cs`
- Di `Program.cs` project Blazor, cari baris `builder.Services.AddScoped(...)`
  untuk HttpClient (biasanya sudah ada template-nya), ubah base address-nya
  jadi alamat BACKEND (bukan alamat Blazor sendiri):
  ```csharp
  builder.Services.AddScoped(sp => new HttpClient
  {
      BaseAddress = new Uri("https://localhost:7050/") // GANTI sesuai port API kamu
  });
  ```

### 5. Jalankan KEDUA project bersamaan
Klik kanan Solution > Properties > Startup Project > pilih "Multiple startup projects"
> set kedua project (Api dan Blazor) jadi "Start". Ini penting — kalau cuma jalanin
salah satu, yang lain nggak akan bisa diakses.

### 6. Buka browser ke alamat Blazor, navigasi ke `/corrosion`

## Error yang sering muncul

- **CORS error di Console browser** ("blocked by CORS policy") — cek lagi
  `Program-snippet.cs`, pastikan origin di `WithOrigins(...)` PERSIS SAMA
  (termasuk http vs https, dan nomor port) dengan alamat Blazor kamu.
- **"Failed to fetch" / Connection refused** — backend API belum jalan.
  Pastikan kedua project di-set "Start" di langkah 5.
- **404 di endpoint /api/corrosion/detect** — cek route di Controller
  (`[Route("api/[controller]")]` + `[HttpPost("detect")]` = `api/corrosion/detect`),
  dan cek `HttpClient.BaseAddress` di frontend sudah benar.
