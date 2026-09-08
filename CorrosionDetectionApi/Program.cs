using CorrosionDetectionApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

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

// Apply any pending EF Core migrations on startup so the database is created/updated automatically.
// Useful for development; for production consider more controlled migration deployment.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CorrosionDbContext>();
    db.Database.Migrate();
}

app.Run();
