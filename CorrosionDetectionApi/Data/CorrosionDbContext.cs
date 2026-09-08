using CorrosionDetection.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CorrosionDetectionApi.Data
{
    public class CorrosionDbContext : IdentityDbContext<ApplicationUser>
    {
        public CorrosionDbContext(DbContextOptions<CorrosionDbContext> options) : base(options) { }
        public DbSet<DetectionSession> DetectionSessions => Set<DetectionSession>();
        public DbSet<DetectionItem> DetectionItems => Set<DetectionItem>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}