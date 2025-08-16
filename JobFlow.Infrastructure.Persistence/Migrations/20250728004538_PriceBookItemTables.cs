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
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PriceBookItems");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "PriceBookItems",
                type: "int",
                nullable: true);

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

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoice",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "PriceBookCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceBookCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceBookItems_CategoryId",
                table: "PriceBookItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceBookCategories_OrganizationId_Name",
                table: "PriceBookCategories",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

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
