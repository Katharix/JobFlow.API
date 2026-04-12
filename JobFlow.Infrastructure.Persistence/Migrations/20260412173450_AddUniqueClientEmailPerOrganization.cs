using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueClientEmailPerOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationClient_OrganizationId",
                table: "OrganizationClient");

            migrationBuilder.AlterColumn<string>(
                name: "EmailAddress",
                table: "OrganizationClient",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationClient_OrganizationId_EmailAddress",
                table: "OrganizationClient",
                columns: new[] { "OrganizationId", "EmailAddress" },
                unique: true,
                filter: "[EmailAddress] IS NOT NULL AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationClient_OrganizationId_EmailAddress",
                table: "OrganizationClient");

            migrationBuilder.AlterColumn<string>(
                name: "EmailAddress",
                table: "OrganizationClient",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationClient_OrganizationId",
                table: "OrganizationClient",
                column: "OrganizationId");
        }
    }
}
