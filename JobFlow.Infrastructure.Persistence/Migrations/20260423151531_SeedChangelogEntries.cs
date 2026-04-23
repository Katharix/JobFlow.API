using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedChangelogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ChangelogEntries",
                columns: new[] { "Id", "Title", "Description", "Version", "Category", "IsPublished", "PublishedAt", "CreatedBy", "UpdatedBy", "CreatedAt", "UpdatedAt", "IsActive", "DeactivatedAtUtc" },
                values: new object[,]
                {
                    {
                        new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                        "Support Hub launched",
                        "The JobFlow Support Hub is now live. Support agents can manage tickets, handle live chat queues, and review customer billing — all from a single dashboard.",
                        "2026.04.0",
                        0, // Feature
                        true,
                        new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                        "system", null,
                        new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                        null, true, null
                    },
                    {
                        new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                        "Live chat queue improvements",
                        "The live chat queue now shows estimated wait times, supports agent pick-up from the queue panel, and persists session state across page refreshes.",
                        "2026.04.1",
                        1, // Improvement
                        true,
                        new DateTimeOffset(2026, 4, 7, 0, 0, 0, TimeSpan.Zero),
                        "system", null,
                        new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc),
                        null, true, null
                    },
                    {
                        new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"),
                        "Billing page — payment events & disputes",
                        "Support agents can now view a full history of payment events and disputes for any organization. Paginated tables with cursor-based navigation are available on the Billing page.",
                        "2026.04.2",
                        0, // Feature
                        true,
                        new DateTimeOffset(2026, 4, 10, 0, 0, 0, TimeSpan.Zero),
                        "system", null,
                        new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                        null, true, null
                    },
                    {
                        new Guid("d4e5f6a7-b8c9-0123-def0-234567890123"),
                        "Employee CSV import",
                        "Organization administrators can now bulk-import employees using a CSV file. The importer validates each row, reports errors inline, and supports role assignment on import.",
                        "2026.04.3",
                        0, // Feature
                        true,
                        new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
                        "system", null,
                        new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc),
                        null, true, null
                    },
                    {
                        new Guid("e5f6a7b8-c9d0-1234-ef01-345678901234"),
                        "JobFlow Grid — sort and search",
                        "All admin data grids now support multi-column sorting and a live search bar. Server-side pagination is available for large datasets.",
                        "2026.04.4",
                        1, // Improvement
                        true,
                        new DateTimeOffset(2026, 4, 18, 0, 0, 0, TimeSpan.Zero),
                        "system", null,
                        new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                        null, true, null
                    },
                    {
                        new Guid("f6a7b8c9-d0e1-2345-f012-456789012345"),
                        "UI spacing and login button branding fix",
                        "Fixed padding on the JobFlow Grid wrapper, increased gap between billing table cards, and updated the Support Hub login button to match brand styling.",
                        "2026.04.5",
                        2, // Fix
                        true,
                        new DateTimeOffset(2026, 4, 23, 0, 0, 0, TimeSpan.Zero),
                        "system", null,
                        new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc),
                        null, true, null
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ChangelogEntries",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                    new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"),
                    new Guid("d4e5f6a7-b8c9-0123-def0-234567890123"),
                    new Guid("e5f6a7b8-c9d0-1234-ef01-345678901234"),
                    new Guid("f6a7b8c9-d0e1-2345-f012-456789012345")
                });
        }
    }
}

