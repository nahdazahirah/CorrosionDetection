using CorrosionDetectionApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Daftarkan service AI
builder.Services.AddSingleton(new CorrosionDetection.Services.CorrosionDetectionService(
    Path.Combine(builder.Environment.ContentRootPath, "Models", "AI", "best.onnx")
));

builder.Services.AddControllers();

// 2. Setup CORS supaya Blazor (beda port) bisa akses API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:7212", "http://localhost:5179")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<CorrosionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// 3. Aktifkan CORS
app.UseCors("AllowBlazorFrontend");

app.UseHttpsRedirection();
app.MapControllers();

app.Run();