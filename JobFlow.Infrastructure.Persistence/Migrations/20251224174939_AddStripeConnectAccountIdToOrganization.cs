using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeConnectAccountIdToOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "EmployeeInvites");

            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "EmployeeInvites");

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "Organization",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShortCode",
                table: "EmployeeInvites",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "EmployeeInvites");

            migrationBuilder.AddColumn<Guid>(
                name: "InviteToken",
                table: "EmployeeInvites",
                nullable: false,
                defaultValueSql: "NEWID()");


            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "EmployeeInvites",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvites_InviteToken",
                table: "EmployeeInvites",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvites_OrganizationId_Email",
                table: "EmployeeInvites",
                columns: new[] { "OrganizationId", "Email" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvites_ShortCode",
                table: "EmployeeInvites",
                column: "ShortCode",
                unique: true,
                filter: "[ShortCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvites_Status_ExpiresAt",
                table: "EmployeeInvites",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeInvites_InviteToken",
                table: "EmployeeInvites");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeInvites_OrganizationId_Email",
                table: "EmployeeInvites");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeInvites_ShortCode",
                table: "EmployeeInvites");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeInvites_Status_ExpiresAt",
                table: "EmployeeInvites");

            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EmployeeInvites");

            migrationBuilder.AlterColumn<string>(
                name: "ShortCode",
                table: "EmployeeInvites",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");


            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "EmployeeInvites");

            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "EmployeeInvites",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "EmployeeInvites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "EmployeeInvites",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
