using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OnBoardingComplete",
                table: "Organization",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Organization",
                keyColumn: "Id",
                keyValue: new Guid("b3b20208-07ae-40a2-971e-adf3bb93fc8c"),
                column: "OnBoardingComplete",
                value: false);

            migrationBuilder.UpdateData(
                table: "Organization",
                keyColumn: "Id",
                keyValue: new Guid("d464b178-a52d-440b-a064-42246f7e0756"),
                column: "OnBoardingComplete",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnBoardingComplete",
                table: "Organization");
        }
    }
}
