using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeRolePresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeRolePresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IndustryKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRolePresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRolePresets_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRolePresetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRolePresetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRolePresetItems_EmployeeRolePresets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "EmployeeRolePresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EmployeeRolePresets",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeactivatedAtUtc", "Description", "IndustryKey", "IsActive", "IsSystem", "Name", "OrganizationId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("1a2b3c4d-1111-1111-1111-111111111111"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Default roles for field service teams.", "home-services", true, true, "Home services", null, null, null },
                    { new Guid("1a2b3c4d-2222-2222-2222-222222222222"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Default roles for creative studios.", "creative", true, true, "Creative", null, null, null },
                    { new Guid("1a2b3c4d-3333-3333-3333-333333333333"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Default roles for consulting teams.", "consulting", true, true, "Consulting", null, null, null },
                    { new Guid("1a2b3c4d-4444-4444-4444-444444444444"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Default roles for repair shops.", "tech-repair", true, true, "Tech repair", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "EmployeeRolePresetItems",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeactivatedAtUtc", "Description", "IsActive", "Name", "PresetId", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("2a2b3c4d-1111-1111-1111-111111111111"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Field technician for on-site work.", true, "Technician", new Guid("1a2b3c4d-1111-1111-1111-111111111111"), 1, null, null },
                    { new Guid("2a2b3c4d-1111-1111-1111-111111111112"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Lead for quality checks and approvals.", true, "Supervisor", new Guid("1a2b3c4d-1111-1111-1111-111111111111"), 2, null, null },
                    { new Guid("2a2b3c4d-1111-1111-1111-111111111113"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Routes schedules and job assignments.", true, "Dispatcher", new Guid("1a2b3c4d-1111-1111-1111-111111111111"), 3, null, null },
                    { new Guid("2a2b3c4d-1111-1111-1111-111111111114"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Back-office support and billing.", true, "Admin", new Guid("1a2b3c4d-1111-1111-1111-111111111111"), 4, null, null },
                    { new Guid("2a2b3c4d-2222-2222-2222-222222222221"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Primary creator and deliverable owner.", true, "Designer", new Guid("1a2b3c4d-2222-2222-2222-222222222222"), 1, null, null },
                    { new Guid("2a2b3c4d-2222-2222-2222-222222222222"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Owns timelines, approvals, and client comms.", true, "Producer", new Guid("1a2b3c4d-2222-2222-2222-222222222222"), 2, null, null },
                    { new Guid("2a2b3c4d-2222-2222-2222-222222222223"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Schedules tasks and supports delivery.", true, "Coordinator", new Guid("1a2b3c4d-2222-2222-2222-222222222222"), 3, null, null },
                    { new Guid("2a2b3c4d-2222-2222-2222-222222222224"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Operations and billing support.", true, "Admin", new Guid("1a2b3c4d-2222-2222-2222-222222222222"), 4, null, null },
                    { new Guid("2a2b3c4d-3333-3333-3333-333333333331"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Client-facing delivery specialist.", true, "Consultant", new Guid("1a2b3c4d-3333-3333-3333-333333333333"), 1, null, null },
                    { new Guid("2a2b3c4d-3333-3333-3333-333333333332"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Owns engagement delivery and quality.", true, "Lead", new Guid("1a2b3c4d-3333-3333-3333-333333333333"), 2, null, null },
                    { new Guid("2a2b3c4d-3333-3333-3333-333333333333"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Plans meetings and follow-ups.", true, "Coordinator", new Guid("1a2b3c4d-3333-3333-3333-333333333333"), 3, null, null },
                    { new Guid("2a2b3c4d-3333-3333-3333-333333333334"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Back-office support and billing.", true, "Admin", new Guid("1a2b3c4d-3333-3333-3333-333333333333"), 4, null, null },
                    { new Guid("2a2b3c4d-4444-4444-4444-444444444441"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Executes diagnostics and repairs.", true, "Repair Tech", new Guid("1a2b3c4d-4444-4444-4444-444444444444"), 1, null, null },
                    { new Guid("2a2b3c4d-4444-4444-4444-444444444442"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Final testing and release approvals.", true, "QA", new Guid("1a2b3c4d-4444-4444-4444-444444444444"), 2, null, null },
                    { new Guid("2a2b3c4d-4444-4444-4444-444444444443"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Client intake and status updates.", true, "Service Advisor", new Guid("1a2b3c4d-4444-4444-4444-444444444444"), 3, null, null },
                    { new Guid("2a2b3c4d-4444-4444-4444-444444444444"), new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Operations and billing support.", true, "Admin", new Guid("1a2b3c4d-4444-4444-4444-444444444444"), 4, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRolePresetItems_PresetId",
                table: "EmployeeRolePresetItems",
                column: "PresetId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRolePresets_OrganizationId",
                table: "EmployeeRolePresets",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeRolePresetItems");

            migrationBuilder.DropTable(
                name: "EmployeeRolePresets");
        }
    }
}
