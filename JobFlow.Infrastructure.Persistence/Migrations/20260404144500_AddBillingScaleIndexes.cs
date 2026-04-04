using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    public partial class AddBillingScaleIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PaymentHistory_EntityId_PaidAt",
                table: "PaymentHistory",
                columns: new[] { "EntityId", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OrganizationId_CreatedAt",
                table: "Invoice",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OrganizationId_Status",
                table: "Invoice",
                columns: new[] { "OrganizationId", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentHistory_EntityId_PaidAt",
                table: "PaymentHistory");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_OrganizationId_CreatedAt",
                table: "Invoice");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_OrganizationId_Status",
                table: "Invoice");
        }
    }
}
