using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicingWorkflowSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoicingWorkflow",
                table: "Job",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "Invoice",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationInvoicingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultWorkflow = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationInvoicingSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_JobId",
                table: "Invoice",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvoicingSettings_OrganizationId",
                table: "OrganizationInvoicingSettings",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_Job_JobId",
                table: "Invoice",
                column: "JobId",
                principalTable: "Job",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_Job_JobId",
                table: "Invoice");

            migrationBuilder.DropTable(
                name: "OrganizationInvoicingSettings");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_JobId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "InvoicingWorkflow",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Invoice");
        }
    }
}
