using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModifyRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("079e4277-0eb2-4222-82e4-5a751ede48f6"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "OrganizationEmployee", "ORGANIZATIONEMPLOYEE" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3da14c58-562a-437a-a2a6-47706b40eb70"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "OrganizationClient", "ORGANIZATIONCLIENT" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5bc0d325-a915-4e17-8184-428ee533cf89"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "KatharixAdmin", "KATHARIXADMIN" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("92193eb2-dba0-433c-814e-9fca95bde016"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "KatharixEmployee", "KATHARIXEMPLOYEE" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("dfe36ebc-bfb5-4583-b68e-59be8ba60fa9"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "SuperAdmin", "SUPERADMIN" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("e88fbbe6-8bdf-4aca-b941-912785a94f0b"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "OrganizationAdmin", "ORGANIZATIONADMIN" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("079e4277-0eb2-4222-82e4-5a751ede48f6"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Organization Employee", "ORGANIZATION EMPLOYEE" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3da14c58-562a-437a-a2a6-47706b40eb70"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Organization Client", "ORGANIZATION CLIENT" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5bc0d325-a915-4e17-8184-428ee533cf89"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Katharix Admin", "KATHARIX ADMIN" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("92193eb2-dba0-433c-814e-9fca95bde016"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Katharix Employee", "KATHARIX EMPLOYEE" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("dfe36ebc-bfb5-4583-b68e-59be8ba60fa9"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Super Admin", "SUPER ADMIN" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("e88fbbe6-8bdf-4aca-b941-912785a94f0b"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Organization Admin", "ORGANIZATION ADMIN" });
        }
    }
}
