using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Models;

namespace PurchaseApproval.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Delegation> Delegations => Set<Delegation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();

            entity.HasMany(u => u.GrantedDelegations)
                .WithOne(d => d.Grantor)
                .HasForeignKey(d => d.GrantorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.ReceivedDelegations)
                .WithOne(d => d.Trustee)
                .HasForeignKey(d => d.TrusteeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PurchaseRequest
        modelBuilder.Entity<PurchaseRequest>(entity =>
        {
            entity.HasIndex(e => e.RequestNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            // 忽略计算属性 TotalAmount
            entity.Ignore(e => e.TotalAmount);
        });

        // ApprovalRecord
        modelBuilder.Entity<ApprovalRecord>(entity =>
        {
            entity.HasIndex(e => e.RequestId);
            entity.HasIndex(e => e.ApproverId);
            entity.HasIndex(e => e.OriginalApproverId);

            entity.HasOne(ar => ar.OriginalApprover)
                .WithMany()
                .HasForeignKey(ar => ar.OriginalApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ActorId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.CorrelationId);
        });

        // Delegation
        modelBuilder.Entity<Delegation>(entity =>
        {
            entity.HasIndex(e => e.GrantorId);
            entity.HasIndex(e => e.TrusteeId);
            entity.HasIndex(e => new { e.GrantorId, e.StartDate, e.EndDate });
            entity.HasIndex(e => e.IsActive);
        });
    }
}
