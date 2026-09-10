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

# Setup: Backend (Web API) + Frontend (Blazor) — 2 Project Terpisah

## Struktur project aktual

Backend:

```text
CorrosionDetectionApi/
├── Controllers/
│   ├── AuthController.cs
│   ├── CorrosionController.cs
│   └── UsersController.cs
├── Data/CorrosionDbContext.cs
├── Migrations/
├── Models/
│   ├── AI/best.onnx
│   ├── ApplicationUser.cs
│   ├── DetectionItem.cs
│   ├── DetectionResponse.cs
│   ├── DetectionResult.cs
│   └── DetectionSession.cs
├── Services/CorrosionDetectionServices.cs
├── Program.cs
├── appsettings.json
└── Properties/launchSettings.json
```

Frontend:

```text
CorrosionDetectionBlazor/
├── Auth/
├── Layout/
├── Models/
├── Pages/
│   ├── Corrosion.razor
│   ├── Dashboard.razor
│   ├── Login.razor
│   ├── Profile.razor
│   ├── Riwayat.razor
│   └── Tentang.razor
├── Shared/NavBar.razor
├── wwwroot/css/app.css
├── wwwroot/js/
└── Program.cs
```

## Prasyarat

- Visual Studio dengan workload **ASP.NET and web development**.
- .NET SDK sesuai `TargetFramework` pada kedua `.csproj`.
- SQL Server atau LocalDB.
- SQL Server Object Explorer atau SQL Server Management Studio.
- EF Core CLI jika migration dijalankan dari PowerShell.

```powershell
dotnet tool install --global dotnet-ef
```

## Database dan migration

Connection string berada di:

```text
CorrosionDetectionApi/appsettings.json
```

Contoh LocalDB:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CorrosionDetectionDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Jalankan dari root solution:

```powershell
dotnet restore
dotnet ef database update --project CorrosionDetectionApi --startup-project CorrosionDetectionApi
```

Daftar migration:

```powershell
dotnet ef migrations list --project CorrosionDetectionApi --startup-project CorrosionDetectionApi
```

Buat migration baru:

```powershell
dotnet ef migrations add NamaMigration --project CorrosionDetectionApi --startup-project CorrosionDetectionApi
```

`Add-Migration` hanya berlaku di Visual Studio Package Manager Console. Di PowerShell gunakan `dotnet ef`.

### Foreign key session dan item

Pastikan `DetectionSession`, `DetectionItem`, `CorrosionDbContext`, migration, dan database menggunakan foreign key yang sama. Konfigurasi relationship secara eksplisit dan jangan mengandalkan shadow property:

```csharp
builder.Entity<DetectionItem>()
    .HasOne(item => item.DetectionSession)
    .WithMany(session => session.Items)
    .HasForeignKey(item => item.SessionId)
    .OnDelete(DeleteBehavior.Cascade);
```

Sebelum memperbaiki migration, periksa schema aktual:

```sql
SELECT
    fk.name AS ForeignKeyName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ForeignKeyColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc
    ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.DetectionItems');
```

Jika migration mencoba menghapus foreign key, index, atau kolom yang sudah tidak ada, jangan jalankan berulang kali. Sesuaikan migration pending dengan schema aktual dan lakukan backup database terlebih dahulu.

## Model AI

Model ONNX wajib berada di:

```text
CorrosionDetectionApi/Models/AI/best.onnx
```

Pastikan file tersebut ikut tersalin ke output saat publish. Service inferensi berada di `Services/CorrosionDetectionServices.cs`.

## Menjalankan aplikasi

### Visual Studio

1. Klik kanan solution → **Properties**.
2. Pilih **Multiple startup projects**.
3. Set `CorrosionDetectionApi` menjadi **Start**.
4. Set `CorrosionDetectionBlazor` menjadi **Start**.
5. Jalankan solution.
6. Buka URL frontend dari `CorrosionDetectionBlazor/Properties/launchSettings.json`.

### Terminal

Terminal backend:

```powershell
dotnet run --project CorrosionDetectionApi
```

Terminal frontend:

```powershell
dotnet run --project CorrosionDetectionBlazor
```

Frontend harus menggunakan URL backend pada `CorrosionDetectionBlazor/Program.cs`:

```csharp
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7050/")
});
```

Ganti port sesuai `CorrosionDetectionApi/Properties/launchSettings.json`.

## CORS dan autentikasi

Origin frontend harus sama persis dengan origin yang diizinkan backend, termasuk protocol dan port. Beberapa endpoint memerlukan JWT dan role `Admin`. Setelah mengubah CORS atau authentication, restart backend.

## Halaman dan endpoint

Halaman frontend:

```text
/login
/corrosion
/dashboard
/riwayat
/profile
/tentang
```

Endpoint utama:

```text
POST   /api/corrosion/detect
GET    /api/corrosion/history
DELETE /api/corrosion/history/{id}
```

Halaman Riwayat memiliki konfirmasi toast sebelum request DELETE dikirim. Jika delete tetap gagal, periksa cascade delete atau hapus `DetectionItems` terlebih dahulu di backend.

## Troubleshooting

### `Add-Migration is not recognized`

Gunakan perintah `dotnet ef` dari PowerShell seperti pada bagian migration.

### `Failed to fetch` atau connection refused

Pastikan kedua project berjalan, `HttpClient.BaseAddress` mengarah ke backend, dan port sesuai `launchSettings.json`.

Jika HTTPS bermasalah:

```powershell
dotnet dev-certs https --trust
```

### CORS error

Periksa protocol, host, dan port URL frontend pada konfigurasi CORS backend.

### Error foreign key atau migration

Periksa `sys.foreign_keys`, `sys.indexes`, `INFORMATION_SCHEMA.COLUMNS`, dan `dbo.__EFMigrationsHistory`. Jangan menghapus data production untuk melewati error migration tanpa backup dan pemeriksaan data.

### Model AI tidak ditemukan

Pastikan `best.onnx` ada di `Models/AI` dan tersedia pada folder output/publish.

## Checklist handover

- [ ] Kedua project dan solution sudah diserahkan.
- [ ] `best.onnx` tersedia dan lisensinya jelas.
- [ ] Connection string dan secret diserahkan melalui kanal aman.
- [ ] Database sudah di-backup.
- [ ] Migration terakhir berhasil diterapkan.
- [ ] Port backend dan frontend sudah dicatat.
- [ ] Login user dan admin sudah diuji.
- [ ] Upload, webcam, dashboard, history, detail, dan delete sudah diuji.
- [ ] CORS dan konfigurasi production sudah ditinjau.
