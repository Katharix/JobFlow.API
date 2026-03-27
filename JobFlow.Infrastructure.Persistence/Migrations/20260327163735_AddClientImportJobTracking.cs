using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientImportJobTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientImportJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ProcessedRows = table.Column<int>(type: "int", nullable: false),
                    SucceededRows = table.Column<int>(type: "int", nullable: false),
                    FailedRows = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientImportJob", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientImportJob_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientImportJobError",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientImportJobError", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientImportJobError_ClientImportJob_ClientImportJobId",
                        column: x => x.ClientImportJobId,
                        principalTable: "ClientImportJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientImportJob_OrganizationId_CreatedAt",
                table: "ClientImportJob",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientImportJob_Status",
                table: "ClientImportJob",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClientImportJobError_ClientImportJobId",
                table: "ClientImportJobError",
                column: "ClientImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientImportJobError_RowNumber",
                table: "ClientImportJobError",
                column: "RowNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientImportJobError");

            migrationBuilder.DropTable(
                name: "ClientImportJob");
        }
    }
}
