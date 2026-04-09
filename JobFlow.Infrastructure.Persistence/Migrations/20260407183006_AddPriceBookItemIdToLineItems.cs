using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceBookItemIdToLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PriceBookItemId",
                table: "InvoiceLineItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceBookItemId",
                table: "EstimateLineItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItem_PriceBookItemId",
                table: "InvoiceLineItem",
                column: "PriceBookItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimateLineItems_PriceBookItemId",
                table: "EstimateLineItems",
                column: "PriceBookItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_EstimateLineItems_PriceBookItems_PriceBookItemId",
                table: "EstimateLineItems",
                column: "PriceBookItemId",
                principalTable: "PriceBookItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLineItem_PriceBookItems_PriceBookItemId",
                table: "InvoiceLineItem",
                column: "PriceBookItemId",
                principalTable: "PriceBookItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EstimateLineItems_PriceBookItems_PriceBookItemId",
                table: "EstimateLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLineItem_PriceBookItems_PriceBookItemId",
                table: "InvoiceLineItem");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLineItem_PriceBookItemId",
                table: "InvoiceLineItem");

            migrationBuilder.DropIndex(
                name: "IX_EstimateLineItems_PriceBookItemId",
                table: "EstimateLineItems");

            migrationBuilder.DropColumn(
                name: "PriceBookItemId",
                table: "InvoiceLineItem");

            migrationBuilder.DropColumn(
                name: "PriceBookItemId",
                table: "EstimateLineItems");
        }
    }
}
