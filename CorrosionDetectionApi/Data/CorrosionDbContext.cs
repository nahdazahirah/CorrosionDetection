using Microsoft.EntityFrameworkCore;
using CorrosionDetection.Models;

namespace CorrosionDetectionApi.Data
{
    public class CorrosionDbContext : DbContext
    {
        public CorrosionDbContext(DbContextOptions<CorrosionDbContext> options) : base(options) { }
        public DbSet<DetectionSession> DetectionSessions => Set<DetectionSession>();
        public DbSet<DetectionItem> DetectionItems => Set<DetectionItem>();
    }
}