using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsConsentToOrganizationClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SmsConsentGiven",
                table: "OrganizationClient",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmsConsentGiven",
                table: "OrganizationClient");
        }
    }
}
