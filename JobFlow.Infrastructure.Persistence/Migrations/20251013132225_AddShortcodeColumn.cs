using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShortcodeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessCount",
                table: "EmployeeInvites",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AccessIpAddress",
                table: "EmployeeInvites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessedAt",
                table: "EmployeeInvites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                table: "EmployeeInvites",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessCount",
                table: "EmployeeInvites");

            migrationBuilder.DropColumn(
                name: "AccessIpAddress",
                table: "EmployeeInvites");

            migrationBuilder.DropColumn(
                name: "AccessedAt",
                table: "EmployeeInvites");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                table: "EmployeeInvites");
        }
    }
}
