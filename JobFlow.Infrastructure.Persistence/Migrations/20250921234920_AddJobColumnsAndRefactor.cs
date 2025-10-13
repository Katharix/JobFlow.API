using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobColumnsAndRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationClientJob");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "Job");

            migrationBuilder.RenameColumn(
                name: "ScheduledTime",
                table: "Job",
                newName: "ScheduledStart");

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "Job",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Address1",
                table: "Job",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Job",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Job",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Job",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationClientId",
                table: "Job",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEnd",
                table: "Job",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Job",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Job",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Job",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobTracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTracking_Job_JobId",
                        column: x => x.JobId,
                        principalTable: "Job",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobTracking_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Job_OrganizationClientId",
                table: "Job",
                column: "OrganizationClientId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTracking_EmployeeId",
                table: "JobTracking",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTracking_JobId",
                table: "JobTracking",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Job_OrganizationClient_OrganizationClientId",
                table: "Job",
                column: "OrganizationClientId",
                principalTable: "OrganizationClient",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Job_OrganizationClient_OrganizationClientId",
                table: "Job");

            migrationBuilder.DropTable(
                name: "JobTracking");

            migrationBuilder.DropIndex(
                name: "IX_Job_OrganizationClientId",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "Address1",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "OrganizationClientId",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ScheduledEnd",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Job");

            migrationBuilder.RenameColumn(
                name: "ScheduledStart",
                table: "Job",
                newName: "ScheduledTime");

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "Job",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "Job",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "OrganizationClientJob",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationClientJob", x => new { x.JobId, x.OrganizationClientId });
                    table.ForeignKey(
                        name: "FK_OrganizationClientJob_Job_JobId",
                        column: x => x.JobId,
                        principalTable: "Job",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationClientJob_OrganizationClient_OrganizationClientId",
                        column: x => x.OrganizationClientId,
                        principalTable: "OrganizationClient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationClientJob_OrganizationClientId",
                table: "OrganizationClientJob",
                column: "OrganizationClientId");
        }
    }
}
