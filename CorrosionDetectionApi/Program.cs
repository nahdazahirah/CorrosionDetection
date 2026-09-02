// ==== Program.cs BACKEND (Web API) ====
// Ini bukan file lengkap, tapi bagian yang perlu kamu TAMBAHKAN/SESUAIKAN
// di Program.cs project Web API kamu.

var builder = WebApplication.CreateBuilder(args);

// 1. Daftarkan service AI kamu (sama seperti sebelumnya)
builder.Services.AddSingleton(new CorrosionDetection.Services.CorrosionDetectionService(
    Path.Combine(builder.Environment.ContentRootPath, "Models", "AI", "best.onnx")
));

builder.Services.AddControllers();

// 2. INI YANG PENTING: setup CORS supaya Blazor (beda port) boleh akses API ini.
//    Ganti "https://localhost:5001" dengan port project Blazor kamu yang sebenarnya
//    (cek di file launchSettings.json project Blazor).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:7212", "http://localhost:5179")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 3. Aktifkan CORS - urutannya PENTING, harus sebelum MapControllers()
app.UseCors("AllowBlazorFrontend");

app.UseHttpsRedirection();
app.MapControllers();

app.Run();