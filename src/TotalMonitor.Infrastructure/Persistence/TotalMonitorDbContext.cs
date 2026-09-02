using Microsoft.EntityFrameworkCore;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.Infrastructure.Persistence;

public sealed class TotalMonitorDbContext(DbContextOptions<TotalMonitorDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<CommunicationSettings> CommunicationSettings => Set<CommunicationSettings>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<MeterConnectionStatus> MeterConnectionStatuses => Set<MeterConnectionStatus>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meter>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80).IsRequired().HasDefaultValue("TOV452");
            e.Property(x => x.ComPort).HasMaxLength(20).IsRequired();
            e.Property(x => x.Parity).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<CommunicationSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ComPort).HasMaxLength(20).IsRequired();
            e.Property(x => x.Parity).HasMaxLength(20).IsRequired();
            e.Property(x => x.StopBits).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<User>(e => { e.HasKey(x => x.Id); e.Property(x => x.UserName).HasMaxLength(120).IsRequired(); e.Property(x => x.DisplayName).HasMaxLength(160).IsRequired(); e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired(); e.Property(x => x.PasswordSalt).HasMaxLength(200).IsRequired(); e.HasIndex(x => x.UserName).IsUnique(); });
        modelBuilder.Entity<Measurement>(e => { e.HasKey(x => x.Id); e.Property(x => x.Variable).HasMaxLength(120).IsRequired(); e.Property(x => x.Unit).HasMaxLength(30).IsRequired(); e.Property(x => x.Value).HasPrecision(18, 6); e.HasIndex(x => new { x.MeterId, x.Timestamp }); e.HasIndex(x => new { x.Variable, x.Timestamp }); });
        modelBuilder.Entity<MeterConnectionStatus>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.MeterId).IsUnique(); });
        modelBuilder.Entity<Role>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(80).IsRequired(); e.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<Permission>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(120).IsRequired(); e.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<UserRole>(e => { e.HasKey(x => new { x.UserId, x.RoleId }); });
        modelBuilder.Entity<RolePermission>(e => { e.HasKey(x => new { x.RoleId, x.PermissionId }); });
        modelBuilder.Entity<AuditLog>(e => { e.HasKey(x => x.Id); e.Property(x => x.Action).HasMaxLength(100).IsRequired(); e.Property(x => x.Description).HasMaxLength(500).IsRequired(); e.HasIndex(x => new { x.UserId, x.Timestamp }); });
    }
}
