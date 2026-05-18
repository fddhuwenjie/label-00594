using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PurchaseApproval.Data;

namespace PurchaseApproval.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("Relational:MaxIdentifierLength", 63)
            .HasAnnotation("ProductVersion", "7.0.0");

        modelBuilder.Entity("PurchaseApproval.Models.AuditLog", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Action")
                .IsRequired()
                .HasColumnType("text");

            b.Property<Guid?>("ActorId")
                .HasColumnType("uuid");

            b.Property<string>("ActorRole")
                .HasColumnType("text");

            b.Property<string>("ActorUsername")
                .HasColumnType("text");

            b.Property<string>("CorrelationId")
                .HasColumnType("text");

            b.Property<string>("Details")
                .HasColumnType("text");

            b.Property<string>("ResourceId")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("ResourceType")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("Result")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.HasKey("Id");

            b.HasIndex("Action");

            b.HasIndex("ActorId");

            b.HasIndex("CorrelationId");

            b.HasIndex("CreatedAt");

            b.ToTable("AuditLogs");
        });

        modelBuilder.Entity("PurchaseApproval.Models.ApprovalRecord", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<int>("Action")
                .HasColumnType("integer");

            b.Property<Guid?>("ActualOperatorId")
                .HasColumnType("uuid");

            b.Property<int>("ApprovalLevel")
                .HasColumnType("integer");

            b.Property<Guid>("ApproverId")
                .HasColumnType("uuid");

            b.Property<string>("Comment")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("character varying(500)");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("RequestId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("ActualOperatorId");

            b.HasIndex("ApproverId");

            b.HasIndex("RequestId");

            b.ToTable("ApprovalRecords");
        });

        modelBuilder.Entity("PurchaseApproval.Models.Delegation", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("DelegateeId")
                .HasColumnType("uuid");

            b.Property<Guid>("DelegatorId")
                .HasColumnType("uuid");

            b.Property<DateTime>("EndDate")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Reason")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("character varying(500)");

            b.Property<DateTime>("StartDate")
                .HasColumnType("timestamp with time zone");

            b.Property<int>("Status")
                .HasColumnType("integer");

            b.Property<DateTime?>("UpdatedAt")
                .HasColumnType("timestamp with time zone");

            b.HasKey("Id");

            b.HasIndex("DelegateeId");

            b.HasIndex("DelegatorId");

            b.HasIndex("Status");

            b.HasIndex("DelegatorId", "Status");

            b.ToTable("Delegations");
        });

        modelBuilder.Entity("PurchaseApproval.Models.Notification", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Content")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<bool>("IsRead")
                .HasColumnType("boolean");

            b.Property<Guid?>("RequestId")
                .HasColumnType("uuid");

            b.Property<string>("Title")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("UserId", "IsRead");

            b.ToTable("Notifications");
        });

        modelBuilder.Entity("PurchaseApproval.Models.PurchaseRequest", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<Guid>("ApplicantId")
                .HasColumnType("uuid");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<int>("CurrentApprovalLevel")
                .HasColumnType("integer");

            b.Property<string>("ItemName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<int>("Quantity")
                .HasColumnType("integer");

            b.Property<string>("Reason")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)");

            b.Property<string>("RequestNumber")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)");

            b.Property<int>("Status")
                .HasColumnType("integer");

            b.Property<decimal>("UnitPrice")
                .HasColumnType("decimal(18,2)");

            b.Property<DateTime>("UpdatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<int>("Urgency")
                .HasColumnType("integer");

            b.Property<string>("WorkflowId")
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.HasKey("Id");

            b.HasIndex("ApplicantId");

            b.HasIndex("CreatedAt");

            b.HasIndex("RequestNumber")
                .IsUnique();

            b.HasIndex("Status");

            b.ToTable("PurchaseRequests");
        });

        modelBuilder.Entity("PurchaseApproval.Models.User", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Department")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("DisplayName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("Password")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<int>("Role")
                .HasColumnType("integer");

            b.Property<string>("Username")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)");

            b.HasKey("Id");

            b.HasIndex("Username")
                .IsUnique();

            b.ToTable("Users");
        });

        modelBuilder.Entity("PurchaseApproval.Models.ApprovalRecord", b =>
        {
            b.HasOne("PurchaseApproval.Models.User", "ActualOperator")
                .WithMany()
                .HasForeignKey("ActualOperatorId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("PurchaseApproval.Models.User", "Approver")
                .WithMany("ApprovalRecords")
                .HasForeignKey("ApproverId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("PurchaseApproval.Models.PurchaseRequest", "Request")
                .WithMany("ApprovalRecords")
                .HasForeignKey("RequestId")
                .OnDelete(DeleteBehavior.Restrict);

            b.Navigation("ActualOperator");

            b.Navigation("Approver");

            b.Navigation("Request");
        });

        modelBuilder.Entity("PurchaseApproval.Models.Delegation", b =>
        {
            b.HasOne("PurchaseApproval.Models.User", "Delegatee")
                .WithMany()
                .HasForeignKey("DelegateeId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("PurchaseApproval.Models.User", "Delegator")
                .WithMany()
                .HasForeignKey("DelegatorId")
                .OnDelete(DeleteBehavior.Restrict);

            b.Navigation("Delegatee");

            b.Navigation("Delegator");
        });

        modelBuilder.Entity("PurchaseApproval.Models.Notification", b =>
        {
            b.HasOne("PurchaseApproval.Models.User", "User")
                .WithMany("Notifications")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict);

            b.Navigation("User");
        });

        modelBuilder.Entity("PurchaseApproval.Models.PurchaseRequest", b =>
        {
            b.HasOne("PurchaseApproval.Models.User", "Applicant")
                .WithMany("PurchaseRequests")
                .HasForeignKey("ApplicantId")
                .OnDelete(DeleteBehavior.Restrict);

            b.Navigation("Applicant");
        });
#pragma warning restore 612, 618
    }
}
