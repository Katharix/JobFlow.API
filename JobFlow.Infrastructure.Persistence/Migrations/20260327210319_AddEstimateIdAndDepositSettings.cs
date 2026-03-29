using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimateIdAndDepositSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercentage",
                table: "OrganizationInvoicingSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "DepositRequired",
                table: "OrganizationInvoicingSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "EstimateId",
                table: "Job",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EstimateId",
                table: "Invoice",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Job_EstimateId",
                table: "Job",
                column: "EstimateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Job_Estimates_EstimateId",
                table: "Job",
                column: "EstimateId",
                principalTable: "Estimates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Job_Estimates_EstimateId",
                table: "Job");

            migrationBuilder.DropIndex(
                name: "IX_Job_EstimateId",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "DepositPercentage",
                table: "OrganizationInvoicingSettings");

            migrationBuilder.DropColumn(
                name: "DepositRequired",
                table: "OrganizationInvoicingSettings");

            migrationBuilder.DropColumn(
                name: "EstimateId",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "EstimateId",
                table: "Invoice");
        }
    }
}
