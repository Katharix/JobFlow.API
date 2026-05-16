using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeInviteRoleAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeInviteRoleAssignments",
                columns: table => new
                {
                    EmployeeInviteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeInviteRoleAssignments", x => new { x.EmployeeInviteId, x.EmployeeRoleId });
                    table.ForeignKey(
                        name: "FK_EmployeeInviteRoleAssignments_EmployeeInvites_EmployeeInviteId",
                        column: x => x.EmployeeInviteId,
                        principalTable: "EmployeeInvites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeInviteRoleAssignments_EmployeeRoles_EmployeeRoleId",
                        column: x => x.EmployeeRoleId,
                        principalTable: "EmployeeRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInviteRoleAssignments_EmployeeRoleId",
                table: "EmployeeInviteRoleAssignments",
                column: "EmployeeRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeInviteRoleAssignments");
        }
    }
}
