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
            entity.HasIndex(e => e.OperatorId);
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
            entity.HasIndex(e => e.DelegatorId);
            entity.HasIndex(e => e.DelegateId);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.EndDate);
            entity.HasIndex(e => e.IsRevoked);
            entity.HasIndex(e => new { e.DelegatorId, e.IsRevoked, e.StartDate, e.EndDate });
        });
    }
}
