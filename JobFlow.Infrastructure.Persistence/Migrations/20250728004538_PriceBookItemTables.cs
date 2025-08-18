using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PriceBookItemTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove old text Category column
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PriceBookItems");

            // Correct: CategoryId is Guid? (nullable) — NOT int
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "PriceBookItems",
                type: "uniqueidentifier",
                nullable: true);

            // Inventory units per sale default 1.0000
            migrationBuilder.AddColumn<decimal>(
                name: "InventoryUnitsPerSale",
                table: "PriceBookItems",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "PriceBookItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Invoice number length constraint
            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoice",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Create PriceBookCategories with Guid PK (no IDENTITY)
            migrationBuilder.CreateTable(
                name: "PriceBookCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceBookCategories", x => x.Id);
                });

            // Unique name per org
            migrationBuilder.CreateIndex(
                name: "IX_PriceBookCategories_OrganizationId_Name",
                table: "PriceBookCategories",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            // FK from items -> categories
            migrationBuilder.CreateIndex(
                name: "IX_PriceBookItems_CategoryId",
                table: "PriceBookItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBookItems_PriceBookCategories_CategoryId",
                table: "PriceBookItems",
                column: "CategoryId",
                principalTable: "PriceBookCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceBookItems_PriceBookCategories_CategoryId",
                table: "PriceBookItems");

            migrationBuilder.DropTable(
                name: "PriceBookCategories");

            migrationBuilder.DropIndex(
                name: "IX_PriceBookItems_CategoryId",
                table: "PriceBookItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "PriceBookItems");

            migrationBuilder.DropColumn(
                name: "InventoryUnitsPerSale",
                table: "PriceBookItems");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PriceBookItems");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "PriceBookItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoice",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
