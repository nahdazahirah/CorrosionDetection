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

            builder.Entity<DetectionItem>()
                .HasOne(item => item.DetectionSession)
                .WithMany(session => session.Items)
                .HasForeignKey(item => item.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}