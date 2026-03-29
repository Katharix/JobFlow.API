using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSquareTokenFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSquareConnected",
                table: "Organization",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SquareMerchantId",
                table: "Organization",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedAccessToken",
                schema: "payment",
                table: "CustomerPaymentProfile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedRefreshToken",
                schema: "payment",
                table: "CustomerPaymentProfile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SquareLocationId",
                schema: "payment",
                table: "CustomerPaymentProfile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAtUtc",
                schema: "payment",
                table: "CustomerPaymentProfile",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSquareConnected",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "SquareMerchantId",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "EncryptedAccessToken",
                schema: "payment",
                table: "CustomerPaymentProfile");

            migrationBuilder.DropColumn(
                name: "EncryptedRefreshToken",
                schema: "payment",
                table: "CustomerPaymentProfile");

            migrationBuilder.DropColumn(
                name: "SquareLocationId",
                schema: "payment",
                table: "CustomerPaymentProfile");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAtUtc",
                schema: "payment",
                table: "CustomerPaymentProfile");
        }
    }
}
