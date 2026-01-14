using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModifyInvoiceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StripeInvoiceId",
                table: "Invoice",
                newName: "ExternalPaymentId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Invoice",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentProvider",
                table: "Invoice",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                table: "Invoice");

            migrationBuilder.RenameColumn(
                name: "ExternalPaymentId",
                table: "Invoice",
                newName: "StripeInvoiceId");
        }
    }
}
