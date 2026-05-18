using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable IDE0058, CA1814

namespace PurchaseApproval.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Password = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
                Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PurchaseRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Urgency = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                WorkflowId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CurrentApprovalLevel = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PurchaseRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_PurchaseRequests_Users_ApplicantId",
                    column: x => x.ApplicantId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ApprovalRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<int>(type: "integer", nullable: false),
                Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ApprovalLevel = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApprovalRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApprovalRecords_PurchaseRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "PurchaseRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ApprovalRecords_Users_ApproverId",
                    column: x => x.ApproverId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "text", nullable: false),
                ResourceType = table.Column<string>(type: "text", nullable: false),
                ResourceId = table.Column<string>(type: "text", nullable: false),
                Result = table.Column<string>(type: "text", nullable: false),
                Details = table.Column<string>(type: "text", nullable: true),
                ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                ActorUsername = table.Column<string>(type: "text", nullable: true),
                ActorRole = table.Column<string>(type: "text", nullable: true),
                CorrelationId = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                IsRead = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notifications_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseRequests_RequestNumber",
            table: "PurchaseRequests",
            column: "RequestNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseRequests_Status",
            table: "PurchaseRequests",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseRequests_CreatedAt",
            table: "PurchaseRequests",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_ApprovalRecords_RequestId",
            table: "ApprovalRecords",
            column: "RequestId");

        migrationBuilder.CreateIndex(
            name: "IX_ApprovalRecords_ApproverId",
            table: "ApprovalRecords",
            column: "ApproverId");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_UserId_IsRead",
            table: "Notifications",
            columns: new[] { "UserId", "IsRead" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CreatedAt",
            table: "AuditLogs",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_ActorId",
            table: "AuditLogs",
            column: "ActorId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Action",
            table: "AuditLogs",
            column: "Action");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CorrelationId",
            table: "AuditLogs",
            column: "CorrelationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ApprovalRecords");
        migrationBuilder.DropTable(name: "AuditLogs");
        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "PurchaseRequests");
        migrationBuilder.DropTable(name: "Users");
    }
}
