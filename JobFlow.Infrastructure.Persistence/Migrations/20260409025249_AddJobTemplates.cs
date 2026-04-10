using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    DefaultInvoicingWorkflow = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_JobTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTemplates_OrganizationType_OrganizationTypeId",
                        column: x => x.OrganizationTypeId,
                        principalTable: "OrganizationType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JobTemplates_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_JobTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTemplateItems_JobTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "JobTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "JobTemplates",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeactivatedAtUtc", "DefaultInvoicingWorkflow", "Description", "IsActive", "IsSystem", "Name", "OrganizationId", "OrganizationTypeId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("3a3b3c3d-0101-0101-0101-010101010101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Full or partial home remodel project.", true, true, "Home renovation", null, new Guid("bf489aa6-db19-42df-82bc-c116bd967e7e"), null, null },
                    { new Guid("3a3b3c3d-0101-0101-0101-010101010102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "New outdoor living space construction.", true, true, "Deck / patio build", null, new Guid("bf489aa6-db19-42df-82bc-c116bd967e7e"), null, null },
                    { new Guid("3a3b3c3d-0202-0202-0202-020202020201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Paint walls, ceilings, and trim for interior rooms.", true, true, "Interior painting", null, new Guid("393a5b3e-323e-4b76-aa86-0d4683ddcd49"), null, null },
                    { new Guid("3a3b3c3d-0202-0202-0202-020202020202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Prep and paint home exterior surfaces.", true, true, "Exterior painting", null, new Guid("393a5b3e-323e-4b76-aa86-0d4683ddcd49"), null, null },
                    { new Guid("3a3b3c3d-0303-0303-0303-030303030301"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Diagnose and fix leaks, clogs, or faults.", true, true, "Plumbing repair", null, new Guid("e01750b0-0d01-4e25-abf7-6efa23509035"), null, null },
                    { new Guid("3a3b3c3d-0303-0303-0303-030303030302"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Remove old unit and install replacement.", true, true, "Water heater install", null, new Guid("e01750b0-0d01-4e25-abf7-6efa23509035"), null, null },
                    { new Guid("3a3b3c3d-0404-0404-0404-040404040401"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Mow, edge, blow, and treat lawn.", true, true, "Lawn maintenance", null, new Guid("fbc6accf-0fb1-4908-b449-14f13b826f24"), null, null },
                    { new Guid("3a3b3c3d-0404-0404-0404-040404040402"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Design beds, select plants, and install.", true, true, "Garden design & install", null, new Guid("fbc6accf-0fb1-4908-b449-14f13b826f24"), null, null },
                    { new Guid("3a3b3c3d-0505-0505-0505-050505050501"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Troubleshoot and fix wiring or fixture issues.", true, true, "Electrical repair", null, new Guid("9362c957-0f41-4c20-9085-c01e449fdda2"), null, null },
                    { new Guid("3a3b3c3d-0505-0505-0505-050505050502"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Upgrade electrical panel for capacity or safety.", true, true, "Panel upgrade", null, new Guid("9362c957-0f41-4c20-9085-c01e449fdda2"), null, null },
                    { new Guid("3a3b3c3d-0606-0606-0606-060606060601"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Measure, build, and install custom shelves.", true, true, "Custom shelving", null, new Guid("8f0d3e93-425b-4a53-b4d2-4c5eb97e490f"), null, null },
                    { new Guid("3a3b3c3d-0606-0606-0606-060606060602"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Install baseboards, crown molding, and casings.", true, true, "Trim & molding install", null, new Guid("8f0d3e93-425b-4a53-b4d2-4c5eb97e490f"), null, null },
                    { new Guid("3a3b3c3d-0707-0707-0707-070707070701"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Seasonal heating/cooling system tune-up.", true, true, "HVAC maintenance", null, new Guid("37fc17e8-0a25-4119-9a71-7d160bb9c7b4"), null, null },
                    { new Guid("3a3b3c3d-0707-0707-0707-070707070702"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Remove old unit and install new AC system.", true, true, "AC install / replacement", null, new Guid("37fc17e8-0a25-4119-9a71-7d160bb9c7b4"), null, null },
                    { new Guid("3a3b3c3d-0808-0808-0808-080808080801"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Fell, section, and haul away tree.", true, true, "Tree removal", null, new Guid("f64f078f-ecfb-4f3e-8640-236219fcf01e"), null, null },
                    { new Guid("3a3b3c3d-0808-0808-0808-080808080802"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Prune branches for health and clearance.", true, true, "Tree trimming", null, new Guid("f64f078f-ecfb-4f3e-8640-236219fcf01e"), null, null },
                    { new Guid("3a3b3c3d-0909-0909-0909-090909090901"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Interior and exterior spray for common pests.", true, true, "General pest treatment", null, new Guid("bf3b9512-8a9c-4a73-9f88-cb914c1573cd"), null, null },
                    { new Guid("3a3b3c3d-0909-0909-0909-090909090902"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Full property inspection and treatment plan.", true, true, "Termite inspection", null, new Guid("bf3b9512-8a9c-4a73-9f88-cb914c1573cd"), null, null },
                    { new Guid("3a3b3c3d-1010-1010-1010-101010101001"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Standard whole-house cleaning visit.", true, true, "Full home clean", null, new Guid("1921d982-22f8-4ed5-b4e3-fca82c5767eb"), null, null },
                    { new Guid("3a3b3c3d-1010-1010-1010-101010101002"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Thorough clean for tenant turnover.", true, true, "Move-out deep clean", null, new Guid("1921d982-22f8-4ed5-b4e3-fca82c5767eb"), null, null },
                    { new Guid("3a3b3c3d-1111-1111-1111-111111111101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Remove all unwanted items from property.", true, true, "Full property cleanout", null, new Guid("408d2185-53b9-493d-8713-938114de90f5"), null, null },
                    { new Guid("3a3b3c3d-1111-1111-1111-111111111102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Clear and haul garage contents.", true, true, "Garage cleanout", null, new Guid("408d2185-53b9-493d-8713-938114de90f5"), null, null },
                    { new Guid("3a3b3c3d-1212-1212-1212-121212121201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Complete interior and exterior detail.", true, true, "Full detail", null, new Guid("33341b2d-957f-4efb-94f7-3a015ae1a718"), null, null },
                    { new Guid("3a3b3c3d-1212-1212-1212-121212121202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Deep clean seats, dash, carpet, and glass.", true, true, "Interior only", null, new Guid("33341b2d-957f-4efb-94f7-3a015ae1a718"), null, null },
                    { new Guid("3a3b3c3d-1313-1313-1313-131313131301"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Install and configure router, switches, and cabling.", true, true, "Network setup", null, new Guid("30530a32-a151-436d-a050-613eac4c22d5"), null, null },
                    { new Guid("3a3b3c3d-1313-1313-1313-131313131302"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Set up desktops, laptops, and peripherals.", true, true, "Workstation deployment", null, new Guid("30530a32-a151-436d-a050-613eac4c22d5"), null, null },
                    { new Guid("3a3b3c3d-1414-1414-1414-141414141401"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Fix miscellaneous issues around the home.", true, true, "General repair", null, new Guid("0f32e14a-5f70-45af-a647-04e59ad52e58"), null, null },
                    { new Guid("3a3b3c3d-1414-1414-1414-141414141402"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Mount and connect lights, fans, or hardware.", true, true, "Fixture install", null, new Guid("0f32e14a-5f70-45af-a647-04e59ad52e58"), null, null },
                    { new Guid("3a3b3c3d-1515-1515-1515-151515151501"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Measure, prep subfloor, and install hardwood.", true, true, "Hardwood install", null, new Guid("09786eab-d69f-45bf-bcec-5f368bd60be7"), null, null },
                    { new Guid("3a3b3c3d-1515-1515-1515-151515151502"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, "Lay tile with proper spacing and grout.", true, true, "Tile install", null, new Guid("09786eab-d69f-45bf-bcec-5f368bd60be7"), null, null }
                });

            migrationBuilder.InsertData(
                table: "JobTemplateItems",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeactivatedAtUtc", "Description", "IsActive", "Name", "SortOrder", "TemplateId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("4a4b4c4d-0101-0101-0101-010101010101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Walk property and scope of work.", true, "Site assessment", 1, new Guid("3a3b3c3d-0101-0101-0101-010101010101"), null, null },
                    { new Guid("4a4b4c4d-0101-0101-0101-010101010102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Remove old materials and prep surfaces.", true, "Demo & prep", 2, new Guid("3a3b3c3d-0101-0101-0101-010101010101"), null, null },
                    { new Guid("4a4b4c4d-0101-0101-0101-010101010103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Construction, paint, and final touches.", true, "Build & finish", 3, new Guid("3a3b3c3d-0101-0101-0101-010101010101"), null, null },
                    { new Guid("4a4b4c4d-0101-0101-0101-010101010201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Draft plan and pull permits.", true, "Design & permits", 1, new Guid("3a3b3c3d-0101-0101-0101-010101010102"), null, null },
                    { new Guid("4a4b4c4d-0101-0101-0101-010101010202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Set footings, frame, and deck boards.", true, "Frame & build", 2, new Guid("3a3b3c3d-0101-0101-0101-010101010102"), null, null },
                    { new Guid("4a4b4c4d-0101-0101-0101-010101010203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Stain/seal and final walkthrough.", true, "Finish & inspect", 3, new Guid("3a3b3c3d-0101-0101-0101-010101010102"), null, null },
                    { new Guid("4a4b4c4d-0202-0202-0202-020202020101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Confirm colors, tape, and cover surfaces.", true, "Color consult & prep", 1, new Guid("3a3b3c3d-0202-0202-0202-020202020201"), null, null },
                    { new Guid("4a4b4c4d-0202-0202-0202-020202020102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Apply primer and two coats.", true, "Prime & paint", 2, new Guid("3a3b3c3d-0202-0202-0202-020202020201"), null, null },
                    { new Guid("4a4b4c4d-0202-0202-0202-020202020103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Detail edges and remove coverings.", true, "Touch-up & clean", 3, new Guid("3a3b3c3d-0202-0202-0202-020202020201"), null, null },
                    { new Guid("4a4b4c4d-0202-0202-0202-020202020201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Clean and prep exterior surfaces.", true, "Power wash & scrape", 1, new Guid("3a3b3c3d-0202-0202-0202-020202020202"), null, null },
                    { new Guid("4a4b4c4d-0202-0202-0202-020202020202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Coat siding, fascia, and trim.", true, "Prime & paint", 2, new Guid("3a3b3c3d-0202-0202-0202-020202020202"), null, null },
                    { new Guid("4a4b4c4d-0202-0202-0202-020202020203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Check coverage and clean site.", true, "Final inspection", 3, new Guid("3a3b3c3d-0202-0202-0202-020202020202"), null, null },
                    { new Guid("4a4b4c4d-0303-0303-0303-030303030101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Locate leak, blockage, or fault.", true, "Diagnose issue", 1, new Guid("3a3b3c3d-0303-0303-0303-030303030301"), null, null },
                    { new Guid("4a4b4c4d-0303-0303-0303-030303030102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Fix or replace the faulty component.", true, "Perform repair", 2, new Guid("3a3b3c3d-0303-0303-0303-030303030301"), null, null },
                    { new Guid("4a4b4c4d-0303-0303-0303-030303030103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Run water, check pressure, tidy area.", true, "Test & clean up", 3, new Guid("3a3b3c3d-0303-0303-0303-030303030301"), null, null },
                    { new Guid("4a4b4c4d-0303-0303-0303-030303030201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Shut off supply and drain tank.", true, "Disconnect old unit", 1, new Guid("3a3b3c3d-0303-0303-0303-030303030302"), null, null },
                    { new Guid("4a4b4c4d-0303-0303-0303-030303030202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Position, connect plumbing and power.", true, "Install new unit", 2, new Guid("3a3b3c3d-0303-0303-0303-030303030302"), null, null },
                    { new Guid("4a4b4c4d-0303-0303-0303-030303030203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Check for leaks and set temperature.", true, "Test & verify", 3, new Guid("3a3b3c3d-0303-0303-0303-030303030302"), null, null },
                    { new Guid("4a4b4c4d-0404-0404-0404-040404040101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Cut grass and trim borders.", true, "Mow & edge", 1, new Guid("3a3b3c3d-0404-0404-0404-040404040401"), null, null },
                    { new Guid("4a4b4c4d-0404-0404-0404-040404040102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Clear clippings from walks and drives.", true, "Blow & clean", 2, new Guid("3a3b3c3d-0404-0404-0404-040404040401"), null, null },
                    { new Guid("4a4b4c4d-0404-0404-0404-040404040103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Apply treatment as needed.", true, "Treat & fertilize", 3, new Guid("3a3b3c3d-0404-0404-0404-040404040401"), null, null },
                    { new Guid("4a4b4c4d-0404-0404-0404-040404040201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Plan beds, paths, and plant selection.", true, "Design layout", 1, new Guid("3a3b3c3d-0404-0404-0404-040404040402"), null, null },
                    { new Guid("4a4b4c4d-0404-0404-0404-040404040202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Amend soil and install plants.", true, "Prep & plant", 2, new Guid("3a3b3c3d-0404-0404-0404-040404040402"), null, null },
                    { new Guid("4a4b4c4d-0404-0404-0404-040404040203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Top-dress and initial watering.", true, "Mulch & water", 3, new Guid("3a3b3c3d-0404-0404-0404-040404040402"), null, null },
                    { new Guid("4a4b4c4d-0505-0505-0505-050505050101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Test circuits and locate issue.", true, "Diagnose fault", 1, new Guid("3a3b3c3d-0505-0505-0505-050505050501"), null, null },
                    { new Guid("4a4b4c4d-0505-0505-0505-050505050102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Replace or repair faulty components.", true, "Repair wiring", 2, new Guid("3a3b3c3d-0505-0505-0505-050505050501"), null, null },
                    { new Guid("4a4b4c4d-0505-0505-0505-050505050103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Confirm power and safety.", true, "Test & verify", 3, new Guid("3a3b3c3d-0505-0505-0505-050505050501"), null, null },
                    { new Guid("4a4b4c4d-0505-0505-0505-050505050201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Kill main and label circuits.", true, "Shut down & disconnect", 1, new Guid("3a3b3c3d-0505-0505-0505-050505050502"), null, null },
                    { new Guid("4a4b4c4d-0505-0505-0505-050505050202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Mount new box and reconnect breakers.", true, "Swap panel", 2, new Guid("3a3b3c3d-0505-0505-0505-050505050502"), null, null },
                    { new Guid("4a4b4c4d-0505-0505-0505-050505050203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Restore power and run tests.", true, "Energize & inspect", 3, new Guid("3a3b3c3d-0505-0505-0505-050505050502"), null, null },
                    { new Guid("4a4b4c4d-0606-0606-0606-060606060101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Take dimensions and plan layout.", true, "Measure & design", 1, new Guid("3a3b3c3d-0606-0606-0606-060606060601"), null, null },
                    { new Guid("4a4b4c4d-0606-0606-0606-060606060102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Build shelving units.", true, "Cut & assemble", 2, new Guid("3a3b3c3d-0606-0606-0606-060606060601"), null, null },
                    { new Guid("4a4b4c4d-0606-0606-0606-060606060103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Mount, sand, and apply finish.", true, "Install & finish", 3, new Guid("3a3b3c3d-0606-0606-0606-060606060601"), null, null },
                    { new Guid("4a4b4c4d-0606-0606-0606-060606060201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Measure runs and miter cut pieces.", true, "Measure & cut", 1, new Guid("3a3b3c3d-0606-0606-0606-060606060602"), null, null },
                    { new Guid("4a4b4c4d-0606-0606-0606-060606060202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Attach molding and set nails.", true, "Nail & set", 2, new Guid("3a3b3c3d-0606-0606-0606-060606060602"), null, null },
                    { new Guid("4a4b4c4d-0606-0606-0606-060606060203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Fill gaps, prime, and paint.", true, "Caulk & paint", 3, new Guid("3a3b3c3d-0606-0606-0606-060606060602"), null, null },
                    { new Guid("4a4b4c4d-0707-0707-0707-070707070101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Check airflow and swap filter.", true, "Inspect & replace filter", 1, new Guid("3a3b3c3d-0707-0707-0707-070707070701"), null, null },
                    { new Guid("4a4b4c4d-0707-0707-0707-070707070102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Remove buildup from outdoor unit.", true, "Clean condenser coils", 2, new Guid("3a3b3c3d-0707-0707-0707-070707070701"), null, null },
                    { new Guid("4a4b4c4d-0707-0707-0707-070707070103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Verify settings and calibration.", true, "Test thermostat", 3, new Guid("3a3b3c3d-0707-0707-0707-070707070701"), null, null },
                    { new Guid("4a4b4c4d-0707-0707-0707-070707070201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Disconnect and haul away.", true, "Remove old unit", 1, new Guid("3a3b3c3d-0707-0707-0707-070707070702"), null, null },
                    { new Guid("4a4b4c4d-0707-0707-0707-070707070202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Set unit, connect lines and electric.", true, "Install new system", 2, new Guid("3a3b3c3d-0707-0707-0707-070707070702"), null, null },
                    { new Guid("4a4b4c4d-0707-0707-0707-070707070203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Add refrigerant and run cycles.", true, "Charge & test", 3, new Guid("3a3b3c3d-0707-0707-0707-070707070702"), null, null },
                    { new Guid("4a4b4c4d-0808-0808-0808-080808080101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Evaluate drop zone and rigging.", true, "Assess & plan", 1, new Guid("3a3b3c3d-0808-0808-0808-080808080801"), null, null },
                    { new Guid("4a4b4c4d-0808-0808-0808-080808080102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Cut tree and process limbs.", true, "Fell & section", 2, new Guid("3a3b3c3d-0808-0808-0808-080808080801"), null, null },
                    { new Guid("4a4b4c4d-0808-0808-0808-080808080103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Remove debris and rake site.", true, "Haul & clean", 3, new Guid("3a3b3c3d-0808-0808-0808-080808080801"), null, null },
                    { new Guid("4a4b4c4d-0808-0808-0808-080808080201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Identify dead or problem branches.", true, "Inspect canopy", 1, new Guid("3a3b3c3d-0808-0808-0808-080808080802"), null, null },
                    { new Guid("4a4b4c4d-0808-0808-0808-080808080202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Cut for shape, clearance, and health.", true, "Prune branches", 2, new Guid("3a3b3c3d-0808-0808-0808-080808080802"), null, null },
                    { new Guid("4a4b4c4d-0808-0808-0808-080808080203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Chip or haul brush.", true, "Clean up", 3, new Guid("3a3b3c3d-0808-0808-0808-080808080802"), null, null },
                    { new Guid("4a4b4c4d-0909-0909-0909-090909090101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Identify pest activity and entry points.", true, "Inspect property", 1, new Guid("3a3b3c3d-0909-0909-0909-090909090901"), null, null },
                    { new Guid("4a4b4c4d-0909-0909-0909-090909090102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Spray interior and exterior perimeter.", true, "Apply treatment", 2, new Guid("3a3b3c3d-0909-0909-0909-090909090901"), null, null },
                    { new Guid("4a4b4c4d-0909-0909-0909-090909090103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Log findings and next visit.", true, "Document & recommend", 3, new Guid("3a3b3c3d-0909-0909-0909-090909090901"), null, null },
                    { new Guid("4a4b4c4d-0909-0909-0909-090909090201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Check foundation, crawl, and attic.", true, "Full property scan", 1, new Guid("3a3b3c3d-0909-0909-0909-090909090902"), null, null },
                    { new Guid("4a4b4c4d-0909-0909-0909-090909090202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Tap and test for damage.", true, "Probe suspect areas", 2, new Guid("3a3b3c3d-0909-0909-0909-090909090902"), null, null },
                    { new Guid("4a4b4c4d-0909-0909-0909-090909090203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Findings and treatment estimate.", true, "Deliver report", 3, new Guid("3a3b3c3d-0909-0909-0909-090909090902"), null, null },
                    { new Guid("4a4b4c4d-1010-1010-1010-101010100101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Assess rooms and special requests.", true, "Walkthrough", 1, new Guid("3a3b3c3d-1010-1010-1010-101010101001"), null, null },
                    { new Guid("4a4b4c4d-1010-1010-1010-101010100102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Scrub fixtures, counters, appliances.", true, "Deep clean kitchen & baths", 2, new Guid("3a3b3c3d-1010-1010-1010-101010101001"), null, null },
                    { new Guid("4a4b4c4d-1010-1010-1010-101010100103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "All hard and carpeted surfaces.", true, "Vacuum & mop", 3, new Guid("3a3b3c3d-1010-1010-1010-101010101001"), null, null },
                    { new Guid("4a4b4c4d-1010-1010-1010-101010100201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Clear remaining items, dust surfaces.", true, "Empty & prep", 1, new Guid("3a3b3c3d-1010-1010-1010-101010101002"), null, null },
                    { new Guid("4a4b4c4d-1010-1010-1010-101010100202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Walls, baseboards, inside cabinets.", true, "Scrub all rooms", 2, new Guid("3a3b3c3d-1010-1010-1010-101010101002"), null, null },
                    { new Guid("4a4b4c4d-1010-1010-1010-101010100203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Verify quality before handoff.", true, "Final walkthrough", 3, new Guid("3a3b3c3d-1010-1010-1010-101010101002"), null, null },
                    { new Guid("4a4b4c4d-1111-1111-1111-111111110101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Tag items and plan truck loads.", true, "Walk & inventory", 1, new Guid("3a3b3c3d-1111-1111-1111-111111111101"), null, null },
                    { new Guid("4a4b4c4d-1111-1111-1111-111111110102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Remove items to truck.", true, "Load & haul", 2, new Guid("3a3b3c3d-1111-1111-1111-111111111101"), null, null },
                    { new Guid("4a4b4c4d-1111-1111-1111-111111110103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Clean site, dump or donate.", true, "Sweep & dispose", 3, new Guid("3a3b3c3d-1111-1111-1111-111111111101"), null, null },
                    { new Guid("4a4b4c4d-1111-1111-1111-111111110201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Separate keep, donate, and trash.", true, "Sort items", 1, new Guid("3a3b3c3d-1111-1111-1111-111111111102"), null, null },
                    { new Guid("4a4b4c4d-1111-1111-1111-111111110202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Move discards to truck.", true, "Load out", 2, new Guid("3a3b3c3d-1111-1111-1111-111111111102"), null, null },
                    { new Guid("4a4b4c4d-1111-1111-1111-111111110203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Broom clean and organize keepers.", true, "Sweep garage", 3, new Guid("3a3b3c3d-1111-1111-1111-111111111102"), null, null },
                    { new Guid("4a4b4c4d-1212-1212-1212-121212120101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Hand wash, clay bar, and dry.", true, "Exterior wash & clay", 1, new Guid("3a3b3c3d-1212-1212-1212-121212121201"), null, null },
                    { new Guid("4a4b4c4d-1212-1212-1212-121212120102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Vacuum, shampoo, and condition.", true, "Interior deep clean", 2, new Guid("3a3b3c3d-1212-1212-1212-121212121201"), null, null },
                    { new Guid("4a4b4c4d-1212-1212-1212-121212120103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Compound, wax, and dress tires.", true, "Polish & protect", 3, new Guid("3a3b3c3d-1212-1212-1212-121212121201"), null, null },
                    { new Guid("4a4b4c4d-1212-1212-1212-121212120201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "All seats, dash, and crevices.", true, "Vacuum & dust", 1, new Guid("3a3b3c3d-1212-1212-1212-121212121202"), null, null },
                    { new Guid("4a4b4c4d-1212-1212-1212-121212120202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Extract and clean fabric or leather.", true, "Shampoo upholstery", 2, new Guid("3a3b3c3d-1212-1212-1212-121212121202"), null, null },
                    { new Guid("4a4b4c4d-1212-1212-1212-121212120203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Clean interior glass and surfaces.", true, "Glass & final wipe", 3, new Guid("3a3b3c3d-1212-1212-1212-121212121202"), null, null },
                    { new Guid("4a4b4c4d-1313-1313-1313-131313130101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Map runs and plan equipment placement.", true, "Site survey", 1, new Guid("3a3b3c3d-1313-1313-1313-131313131301"), null, null },
                    { new Guid("4a4b4c4d-1313-1313-1313-131313130102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Pull cable and install hardware.", true, "Run cable & mount", 2, new Guid("3a3b3c3d-1313-1313-1313-131313131301"), null, null },
                    { new Guid("4a4b4c4d-1313-1313-1313-131313130103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Set up network and verify connectivity.", true, "Configure & test", 3, new Guid("3a3b3c3d-1313-1313-1313-131313131301"), null, null },
                    { new Guid("4a4b4c4d-1313-1313-1313-131313130201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Prep hardware and install OS/software.", true, "Unbox & image", 1, new Guid("3a3b3c3d-1313-1313-1313-131313131302"), null, null },
                    { new Guid("4a4b4c4d-1313-1313-1313-131313130202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Place at desk and join network.", true, "Deploy & connect", 2, new Guid("3a3b3c3d-1313-1313-1313-131313131302"), null, null },
                    { new Guid("4a4b4c4d-1313-1313-1313-131313130203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Verify apps and orient user.", true, "User walkthrough", 3, new Guid("3a3b3c3d-1313-1313-1313-131313131302"), null, null },
                    { new Guid("4a4b4c4d-1414-1414-1414-141414140101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Inspect problem area and plan fix.", true, "Assess issue", 1, new Guid("3a3b3c3d-1414-1414-1414-141414141401"), null, null },
                    { new Guid("4a4b4c4d-1414-1414-1414-141414140102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Fix or replace components.", true, "Repair", 2, new Guid("3a3b3c3d-1414-1414-1414-141414141401"), null, null },
                    { new Guid("4a4b4c4d-1414-1414-1414-141414140103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Test fix and tidy work area.", true, "Verify & clean up", 3, new Guid("3a3b3c3d-1414-1414-1414-141414141401"), null, null },
                    { new Guid("4a4b4c4d-1414-1414-1414-141414140201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Mark placement and run wiring if needed.", true, "Prep location", 1, new Guid("3a3b3c3d-1414-1414-1414-141414141402"), null, null },
                    { new Guid("4a4b4c4d-1414-1414-1414-141414140202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Secure and connect.", true, "Mount fixture", 2, new Guid("3a3b3c3d-1414-1414-1414-141414141402"), null, null },
                    { new Guid("4a4b4c4d-1414-1414-1414-141414140203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Confirm operation and patch holes.", true, "Test & finish", 3, new Guid("3a3b3c3d-1414-1414-1414-141414141402"), null, null },
                    { new Guid("4a4b4c4d-1515-1515-1515-151515150101"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Level, clean, and lay moisture barrier.", true, "Prep subfloor", 1, new Guid("3a3b3c3d-1515-1515-1515-151515151501"), null, null },
                    { new Guid("4a4b4c4d-1515-1515-1515-151515150102"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Lay and nail or click into place.", true, "Install planks", 2, new Guid("3a3b3c3d-1515-1515-1515-151515151501"), null, null },
                    { new Guid("4a4b4c4d-1515-1515-1515-151515150103"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Install transitions and clean.", true, "Trim & finish", 3, new Guid("3a3b3c3d-1515-1515-1515-151515151501"), null, null },
                    { new Guid("4a4b4c4d-1515-1515-1515-151515150201"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Dry-fit pattern and mix thinset.", true, "Layout & prep", 1, new Guid("3a3b3c3d-1515-1515-1515-151515151502"), null, null },
                    { new Guid("4a4b4c4d-1515-1515-1515-151515150202"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Lay tiles with spacers.", true, "Set tile", 2, new Guid("3a3b3c3d-1515-1515-1515-151515151502"), null, null },
                    { new Guid("4a4b4c4d-1515-1515-1515-151515150203"), new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Fill joints, clean haze, and seal.", true, "Grout & seal", 3, new Guid("3a3b3c3d-1515-1515-1515-151515151502"), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplateItems_TemplateId",
                table: "JobTemplateItems",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_OrganizationId",
                table: "JobTemplates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_OrganizationTypeId",
                table: "JobTemplates",
                column: "OrganizationTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobTemplateItems");

            migrationBuilder.DropTable(
                name: "JobTemplates");
        }
    }
}
