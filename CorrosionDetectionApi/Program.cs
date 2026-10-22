using CorrosionDetectionApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using CorrosionDetection.Models;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Mendaftar service AI
builder.Services.AddSingleton(new CorrosionDetection.Services.CorrosionDetectionService(
    Path.Combine(builder.Environment.ContentRootPath, "Models", "AI", "best.onnx")
));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Setup CORS supaya Blazor (beda port) bisa akses API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:7212", "http://localhost:5179")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<CorrosionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(option=>
{
    option.Password.RequireDigit = true;
    option.Password.RequiredLength = 6;
    option.Password.RequireNonAlphanumeric = false;
    option.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<CorrosionDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// 1. Migrate database DULU — supaya semua tabel (termasuk Identity) sudah ada
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CorrosionDbContext>();
    db.Database.Migrate();

    // Seed data sampel Pertamina jika database masih kosong
    if (!db.DetectionSessions.Any())
    {
        var sampleSessions = new List<DetectionSession>
        {
            new DetectionSession
            {
                Timestamp = DateTime.Now.AddHours(-3),
                SourceType = "upload",
                ImageWidth = 1920,
                ImageHeight = 1080,
                DetectionCount = 3,
                UnitName = "RU VI Balongan",
                Area = "CDU-01 Process Area",
                EquipmentTag = "PIPE-4\"-HC-1004",
                EquipmentType = "Process Piping (API 570)",
                InspectionMethod = "Survei Drone / UAV",
                InspectorName = "Ir. Handoko Wibowo",
                InspectorNip = "NIP. 801124",
                SeverityGrade = "Critical",
                WorkOrderStatus = "Perlu Work Order",
                WorkOrderNumber = "WO-EMERG-20261022-742",
                Notes = "Korosi berat teridentifikasi pada elbow pipa dekat header uap. Berpotensi terjadi perforasi.",
                Recommendation = "BAHAYA INTEGRITAS STRUKTUR (CRITICAL HAZARD): Indikasi degradasi parah dengan risiko perforasi (kebocoran hidrokarbon). WAJIB lakukan evaluasi fitness-for-service (API 579), pasang temporary engineered clamp, dan terbitkan Emergency Work Order (SAP PM Tipe M2) dalam 24 jam.",
                Items = new List<DetectionItem>
                {
                    new DetectionItem { X = 450, Y = 280, Width = 320, Height = 240, Confidence = 0.89f, AreaPercentage = 34.2f },
                    new DetectionItem { X = 820, Y = 310, Width = 180, Height = 140, Confidence = 0.78f, AreaPercentage = 16.5f },
                    new DetectionItem { X = 1100, Y = 400, Width = 120, Height = 90, Confidence = 0.72f, AreaPercentage = 8.1f }
                }
            },
            new DetectionSession
            {
                Timestamp = DateTime.Now.AddDays(-1).AddHours(2),
                SourceType = "upload",
                ImageWidth = 1920,
                ImageHeight = 1080,
                DetectionCount = 2,
                UnitName = "RU IV Cilacap",
                Area = "Tank Farm Area 3",
                EquipmentTag = "11-TK-102A",
                EquipmentType = "Storage Tank (API 653)",
                InspectionMethod = "Patroli Lapangan (Tablet)",
                InspectorName = "Pratama Bayu",
                InspectorNip = "NIP. 782419",
                SeverityGrade = "Severe",
                WorkOrderStatus = "Rekomendasi Pemeliharaan",
                WorkOrderNumber = "WO-RU4-2026-0089",
                Notes = "Pengelupasan coating primer dan scale rust pada shell course ke-2.",
                Recommendation = "Korosi aktif dengan pengelupasan kerak karat tebal (Scale Rust / Pitting Initiation). WAJIB lakukan verifikasi Ultrasonic Thickness (UT) Gauging dalam 14 hari untuk memastikan sisa ketebalan aman (Tmin API 570/653). Terbitkan Notifikasi Pemeliharaan SAP PM (M1).",
                Items = new List<DetectionItem>
                {
                    new DetectionItem { X = 600, Y = 350, Width = 280, Height = 210, Confidence = 0.84f, AreaPercentage = 22.4f },
                    new DetectionItem { X = 950, Y = 420, Width = 160, Height = 120, Confidence = 0.69f, AreaPercentage = 11.2f }
                }
            },
            new DetectionSession
            {
                Timestamp = DateTime.Now.AddDays(-2).AddHours(-4),
                SourceType = "upload",
                ImageWidth = 1280,
                ImageHeight = 720,
                DetectionCount = 1,
                UnitName = "RU II Dumai",
                Area = "Hydrocracker Unit",
                EquipmentTag = "V-301-SEPARATOR",
                EquipmentType = "Pressure Vessel (API 510)",
                InspectionMethod = "Snapshot Berkala NVR (CAM-RU2-HC-04)",
                InspectorName = "Alya Kinanti",
                InspectorNip = "NIP. 791823",
                SeverityGrade = "Moderate",
                WorkOrderStatus = "Normal / Monitor",
                WorkOrderNumber = null,
                Notes = "Flaking cat pelindung pada saddle support.",
                Recommendation = "Kerusakan coating primer/topcoat dan korosi atmosferik lokal (ISO 12944 Ri 2-3). Rekomendasi spot blasting (SSPC-SP 10 / Sa 2.5) dan touch-up protective coating pada jadwal preventive maintenance berikutnya.",
                Items = new List<DetectionItem>
                {
                    new DetectionItem { X = 380, Y = 220, Width = 150, Height = 110, Confidence = 0.76f, AreaPercentage = 12.8f }
                }
            },
            new DetectionSession
            {
                Timestamp = DateTime.Now.AddDays(-3).AddHours(1),
                SourceType = "upload",
                ImageWidth = 1920,
                ImageHeight = 1080,
                DetectionCount = 1,
                UnitName = "Pertamina Hulu Rokan (PHR)",
                Area = "Wellpad Bravo Manifold",
                EquipmentTag = "MNF-WP-05",
                EquipmentType = "Process Piping (API 570)",
                InspectionMethod = "Patroli Lapangan (Tablet)",
                InspectorName = "Nahda Zahirah",
                InspectorNip = "NIP. 782419",
                SeverityGrade = "Low",
                WorkOrderStatus = "Normal / Monitor",
                WorkOrderNumber = null,
                Notes = "Noda oksidasi ringan pada baut flange.",
                Recommendation = "Korosi permukaan ringan / noda oksidasi (ISO 12944 Ri 1). Belum ada indikasi pitting mendalam. Catat ke CML (Condition Monitoring Location) dan monitor pada siklus 3 bulan.",
                Items = new List<DetectionItem>
                {
                    new DetectionItem { X = 500, Y = 400, Width = 80, Height = 60, Confidence = 0.65f, AreaPercentage = 3.8f }
                }
            }
        };

        db.DetectionSessions.AddRange(sampleSessions);
        db.SaveChanges();
    }
}

// 2. BARU seed roles & admin user
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    string[] roles = { "Admin", "Operator", "Viewer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    string adminEmail = config["AdminSeed:Email"] ?? "admin@example.com";
    string? adminPassword = config["AdminSeed:Password"];

    if (string.IsNullOrEmpty(adminPassword))
    {
        Console.WriteLine("PERINGATAN: AdminSeed:Password belum di-set. Akun admin default tidak dibuat.");
    }
    else if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            Console.WriteLine($"Gagal bikin admin: {errors}");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Urutan middleware yang benar:
// 1. HTTPS Redirect (harus paling awal supaya header Authorization tidak hilang saat redirect)
app.UseHttpsRedirection();

// 2. CORS (sebelum Authentication)
app.UseCors("AllowBlazorFrontend");

// 3. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
