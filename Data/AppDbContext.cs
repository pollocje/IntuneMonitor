using IntuneMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntuneMonitor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<EnrollmentRecord> EnrollmentRecords => Set<EnrollmentRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Tenant>(t =>
        {
            t.HasKey(x => x.Id);
            t.HasIndex(x => x.MicrosoftTenantId).IsUnique();
            t.Property(x => x.SubscriptionStatus).HasConversion<string>();
        });

        model.Entity<AppUser>(u =>
        {
            u.HasKey(x => x.Id);
            u.HasIndex(x => x.Email).IsUnique();
            u.Property(x => x.Role).HasConversion<string>();
            u.HasOne(x => x.Tenant)
             .WithMany(x => x.Users)
             .HasForeignKey(x => x.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<EnrollmentRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.DeviceId });
            e.HasOne(x => x.Tenant)
             .WithMany(x => x.EnrollmentRecords)
             .HasForeignKey(x => x.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
