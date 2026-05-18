using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable IDE0058, CA1814

namespace PurchaseApproval.Migrations;

public partial class AddDelegationSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ActualOperatorId",
            table: "ApprovalRecords",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Delegations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DelegatorId = table.Column<Guid>(type: "uuid", nullable: false),
                DelegateeId = table.Column<Guid>(type: "uuid", nullable: false),
                StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Delegations", x => x.Id);
                table.ForeignKey(
                    name: "FK_Delegations_Users_DelegatorId",
                    column: x => x.DelegatorId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Delegations_Users_DelegateeId",
                    column: x => x.DelegateeId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ApprovalRecords_ActualOperatorId",
            table: "ApprovalRecords",
            column: "ActualOperatorId");

        migrationBuilder.CreateIndex(
            name: "IX_Delegations_DelegatorId",
            table: "Delegations",
            column: "DelegatorId");

        migrationBuilder.CreateIndex(
            name: "IX_Delegations_DelegateeId",
            table: "Delegations",
            column: "DelegateeId");

        migrationBuilder.CreateIndex(
            name: "IX_Delegations_Status",
            table: "Delegations",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Delegations_DelegatorId_Status",
            table: "Delegations",
            columns: new[] { "DelegatorId", "Status" });

        migrationBuilder.AddForeignKey(
            name: "FK_ApprovalRecords_Users_ActualOperatorId",
            table: "ApprovalRecords",
            column: "ActualOperatorId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ApprovalRecords_Users_ActualOperatorId",
            table: "ApprovalRecords");

        migrationBuilder.DropIndex(
            name: "IX_ApprovalRecords_ActualOperatorId",
            table: "ApprovalRecords");

        migrationBuilder.DropColumn(
            name: "ActualOperatorId",
            table: "ApprovalRecords");

        migrationBuilder.DropTable(name: "Delegations");
    }
}
