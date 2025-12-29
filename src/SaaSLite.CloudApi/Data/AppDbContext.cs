using Microsoft.EntityFrameworkCore;
using SaaSLite.CloudApi.Data.Entities;

namespace SaaSLite.CloudApi.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ResultEntity> Results => Set<ResultEntity>();
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ResultEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ResultId).IsUnique();
            b.HasIndex(x => new { x.SiteId, x.DeviceId, x.CollectedAtUtc });
            b.Property(x => x.ResultId).HasMaxLength(128);
            b.Property(x => x.SiteId).HasMaxLength(64);
            b.Property(x => x.DeviceId).HasMaxLength(128);
            b.Property(x => x.EdgeAgentId).HasMaxLength(128);
            b.Property(x => x.TestCode).HasMaxLength(64);
            b.Property(x => x.PatientId).HasMaxLength(64);
            b.Property(x => x.OperatorId).HasMaxLength(64);
        });

        modelBuilder.Entity<DeviceEntity>(b =>
        {
            b.HasKey(x => x.DeviceId);
            b.Property(x => x.DeviceId).HasMaxLength(128);
            b.Property(x => x.SiteId).HasMaxLength(64);
            b.Property(x => x.DisplayName).HasMaxLength(128);
            b.Property(x => x.DeviceType).HasMaxLength(64);
            b.HasIndex(x => new { x.SiteId, x.LastSeenUtc });
        });
    }
}
