using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanName",
                schema: "payment",
                table: "SubscriptionRecord",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "OrganizationType",
                columns: new[] { "Id", "TypeName" },
                values: new object[,]
                {
                    { new Guid("09786eab-d69f-45bf-bcec-5f368bd60be7"), "Flooring" },
                    { new Guid("0f32e14a-5f70-45af-a647-04e59ad52e58"), "Handyman" },
                    { new Guid("906a2bdb-4cc6-4e49-acc3-1bd63fb82611"), "Other" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OrganizationType",
                keyColumn: "Id",
                keyValue: new Guid("09786eab-d69f-45bf-bcec-5f368bd60be7"));

            migrationBuilder.DeleteData(
                table: "OrganizationType",
                keyColumn: "Id",
                keyValue: new Guid("0f32e14a-5f70-45af-a647-04e59ad52e58"));

            migrationBuilder.DeleteData(
                table: "OrganizationType",
                keyColumn: "Id",
                keyValue: new Guid("906a2bdb-4cc6-4e49-acc3-1bd63fb82611"));

            migrationBuilder.DropColumn(
                name: "PlanName",
                schema: "payment",
                table: "SubscriptionRecord");
        }
    }
}
