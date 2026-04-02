using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemRolesAndRegistrationRoleGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = '079e4277-0eb2-4222-82e4-5a751ede48f6')
                    INSERT INTO [Roles] ([Id], [Name]) VALUES ('079e4277-0eb2-4222-82e4-5a751ede48f6', 'OrganizationEmployee');

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = '3da14c58-562a-437a-a2a6-47706b40eb70')
                    INSERT INTO [Roles] ([Id], [Name]) VALUES ('3da14c58-562a-437a-a2a6-47706b40eb70', 'OrganizationClient');

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = '5bc0d325-a915-4e17-8184-428ee533cf89')
                    INSERT INTO [Roles] ([Id], [Name]) VALUES ('5bc0d325-a915-4e17-8184-428ee533cf89', 'KatharixAdmin');

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = '92193eb2-dba0-433c-814e-9fca95bde016')
                    INSERT INTO [Roles] ([Id], [Name]) VALUES ('92193eb2-dba0-433c-814e-9fca95bde016', 'KatharixEmployee');

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 'dfe36ebc-bfb5-4583-b68e-59be8ba60fa9')
                    INSERT INTO [Roles] ([Id], [Name]) VALUES ('dfe36ebc-bfb5-4583-b68e-59be8ba60fa9', 'SuperAdmin');

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 'e88fbbe6-8bdf-4aca-b941-912785a94f0b')
                    INSERT INTO [Roles] ([Id], [Name]) VALUES ('e88fbbe6-8bdf-4aca-b941-912785a94f0b', 'OrganizationAdmin');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("079e4277-0eb2-4222-82e4-5a751ede48f6"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3da14c58-562a-437a-a2a6-47706b40eb70"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5bc0d325-a915-4e17-8184-428ee533cf89"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("92193eb2-dba0-433c-814e-9fca95bde016"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("dfe36ebc-bfb5-4583-b68e-59be8ba60fa9"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("e88fbbe6-8bdf-4aca-b941-912785a94f0b"));
        }
    }
}
