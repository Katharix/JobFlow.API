using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingDurationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                schema: "site",
                table: "PricingTier");

            migrationBuilder.AddColumn<int>(
                name: "DurationType",
                schema: "site",
                table: "PricingTier",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationType",
                schema: "site",
                table: "PricingTier");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                schema: "site",
                table: "PricingTier",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
