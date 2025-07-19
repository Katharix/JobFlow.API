using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationDefaultTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTaxRate",
                table: "Organization",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "EnableTax",
                table: "Organization",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Organization",
                keyColumn: "Id",
                keyValue: new Guid("b3b20208-07ae-40a2-971e-adf3bb93fc8c"),
                columns: new[] { "DefaultTaxRate", "EnableTax" },
                values: new object[] { 0.00m, false });

            migrationBuilder.UpdateData(
                table: "Organization",
                keyColumn: "Id",
                keyValue: new Guid("d464b178-a52d-440b-a064-42246f7e0756"),
                columns: new[] { "DefaultTaxRate", "EnableTax" },
                values: new object[] { 0.00m, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultTaxRate",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "EnableTax",
                table: "Organization");
        }
    }
}
