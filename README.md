# Corrosion Detection

Aplikasi deteksi korosi dengan dua project terpisah: ASP.NET Core Web API sebagai backend dan Blazor WebAssembly sebagai frontend.

## Struktur project saat ini

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

CorrosionDetectionBlazor/
├── Auth/
│   ├── AuthorizedMessageHandler.cs
│   └── CustomAuthStateProvider.cs
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
├── wwwroot/
│   ├── css/app.css
│   └── js/
└── Program.cs
```

## Prasyarat

- Visual Studio dengan workload ASP.NET and web development.
- .NET SDK sesuai `TargetFramework` di kedua `.csproj`.
- SQL Server atau LocalDB.
- SQL Server Object Explorer atau SQL Server Management Studio.
- EF Core CLI jika migration dijalankan dari terminal:

```powershell
dotnet tool install --global dotnet-ef
```

Project API menggunakan paket EF Core SQL Server, EF Core Design/Tools, ONNX Runtime, ImageSharp, ASP.NET Identity, dan JWT authentication. Project frontend menggunakan Blazor WebAssembly.

## Konfigurasi database

Connection string backend berada di:

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

Gunakan User Secrets atau environment variables untuk password dan secret production. Jangan commit credential ke Git.

## Setup database dan migration

Dari root solution:

```powershell
dotnet restore
dotnet ef database update --project CorrosionDetectionApi --startup-project CorrosionDetectionApi
```

Perintah migration:

```powershell
dotnet ef migrations list --project CorrosionDetectionApi --startup-project CorrosionDetectionApi
dotnet ef migrations add NamaMigration --project CorrosionDetectionApi --startup-project CorrosionDetectionApi
dotnet ef database update --project CorrosionDetectionApi --startup-project CorrosionDetectionApi
```

`Add-Migration` hanya tersedia di Visual Studio Package Manager Console. Pada PowerShell biasa gunakan `dotnet ef` seperti di atas.

### Relasi DetectionSession dan DetectionItem

Entity, `CorrosionDbContext`, migration, dan database harus menggunakan foreign key yang sama. Gunakan konfigurasi eksplisit, bukan shadow property:

```csharp
builder.Entity<DetectionItem>()
    .HasOne(item => item.DetectionSession)
    .WithMany(session => session.Items)
    .HasForeignKey(item => item.SessionId)
    .OnDelete(DeleteBehavior.Cascade);
```

Sebelum memperbaiki schema database, backup database dan periksa kondisi aktual:

```sql
SELECT
    fk.name AS ForeignKeyName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ForeignKeyColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc
    ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.DetectionItems');
```

Jika migration mengandung operasi `DropForeignKey`, `DropIndex`, atau `DropColumn` untuk objek yang sudah tidak ada, migration harus disesuaikan sebelum `database update`. Jangan menghapus data production hanya untuk melewati error migration.

## Model AI

Model ONNX wajib berada di:

```text
CorrosionDetectionApi/Models/AI/best.onnx
```

Service inferensi berada di `Services/CorrosionDetectionServices.cs`. Saat publish, pastikan `best.onnx` ikut disalin ke output/publish.

## Menjalankan aplikasi

### Visual Studio

1. Buka solution.
2. Klik kanan solution → Properties.
3. Pilih **Multiple startup projects**.
4. Set `CorrosionDetectionApi` dan `CorrosionDetectionBlazor` menjadi **Start**.
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

Frontend harus menggunakan URL backend di `CorrosionDetectionBlazor/Program.cs`, bukan URL frontend:

```csharp
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7050/")
});
```

Ganti port sesuai `CorrosionDetectionApi/Properties/launchSettings.json`.

## CORS

Origin frontend harus terdaftar di konfigurasi CORS backend dan harus sama persis, termasuk protocol dan port. Contoh:

```text
https://localhost:xxxx
http://localhost:xxxx
```

Setiap perubahan CORS memerlukan restart backend. Hindari `AllowAnyOrigin` bersama credentials pada production.

## Halaman frontend

- `/login` — login/autentikasi.
- `/corrosion` — upload gambar dan deteksi webcam.
- `/dashboard` — ringkasan deteksi.
- `/riwayat` — filter, detail, dan hapus riwayat session.
- `/profile` — profil pengguna.
- `/tentang` — informasi aplikasi.

Halaman Riwayat memiliki konfirmasi sebelum mengirim request DELETE. Penghapusan tetap membutuhkan foreign key database yang konsisten atau penghapusan child records di backend.

## Controller dan endpoint utama

- `AuthController` — autentikasi.
- `CorrosionController` — deteksi dan history.
- `UsersController` — user/admin.

Endpoint yang digunakan oleh frontend antara lain:

```text
POST   /api/corrosion/detect
GET    /api/corrosion/history
DELETE /api/corrosion/history/{id}
```

Periksa route attribute di controller jika endpoint berubah. Endpoint tertentu membutuhkan JWT dan role `Admin`.

## Troubleshooting

### `Add-Migration is not recognized`

Gunakan:

```powershell
dotnet ef migrations add NamaMigration --project CorrosionDetectionApi --startup-project CorrosionDetectionApi
```

### CORS error

Periksa URL frontend, URL backend, protocol, dan port. Pastikan origin yang diizinkan sama persis dengan URL di browser.

### `Failed to fetch` atau connection refused

Pastikan kedua project berjalan, `HttpClient.BaseAddress` menunjuk ke backend, dan HTTPS development certificate dipercaya:

```powershell
dotnet dev-certs https --trust
```

### Foreign key atau migration gagal

Periksa `sys.foreign_keys`, `sys.indexes`, `INFORMATION_SCHEMA.COLUMNS`, dan `dbo.__EFMigrationsHistory`. Migration yang gagal biasanya tidak tercatat karena transaksi di-rollback, tetapi tetap periksa history sebelum mengulang.

### Model AI tidak ditemukan

Pastikan `best.onnx` ada di `Models/AI` dan ikut dalam output publish.

## Checklist serah-terima

- [ ] Kedua project dan solution sudah diserahkan.
- [ ] `best.onnx` tersedia dan lisensinya jelas.
- [ ] Connection string dan secret diserahkan melalui kanal aman.
- [ ] Database sudah di-backup.
- [ ] Migration terakhir sudah diterapkan pada environment tujuan.
- [ ] Port backend dan frontend sudah dicatat.
- [ ] Login user dan admin sudah diuji.
- [ ] Upload, webcam, dashboard, history, detail, dan delete sudah diuji.
- [ ] CORS dan konfigurasi production sudah ditinjau.