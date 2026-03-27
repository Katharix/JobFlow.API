using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUpAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FollowUpSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    StopOnClientReply = table.Column<bool>(type: "bit", nullable: false),
                    DefaultChannel = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowUpSequenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StopReason = table.Column<int>(type: "int", nullable: false),
                    NextStepOrder = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUpRuns_FollowUpSequences_FollowUpSequenceId",
                        column: x => x.FollowUpSequenceId,
                        principalTable: "FollowUpSequences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowUpSequenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    DelayHours = table.Column<int>(type: "int", nullable: false),
                    ChannelOverride = table.Column<int>(type: "int", nullable: true),
                    MessageTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsEscalation = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUpSteps_FollowUpSequences_FollowUpSequenceId",
                        column: x => x.FollowUpSequenceId,
                        principalTable: "FollowUpSequences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowUpRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    ScheduledFor = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    WasSent = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUpExecutionLogs_FollowUpRuns_FollowUpRunId",
                        column: x => x.FollowUpRunId,
                        principalTable: "FollowUpRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpExecutionLogs_FollowUpRunId_StepOrder",
                table: "FollowUpExecutionLogs",
                columns: new[] { "FollowUpRunId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpRuns_FollowUpSequenceId_OrganizationClientId_Status",
                table: "FollowUpRuns",
                columns: new[] { "FollowUpSequenceId", "OrganizationClientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpRuns_OrganizationId_SequenceType_TriggerEntityId_Status",
                table: "FollowUpRuns",
                columns: new[] { "OrganizationId", "SequenceType", "TriggerEntityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpSequences_OrganizationId_SequenceType_IsActive",
                table: "FollowUpSequences",
                columns: new[] { "OrganizationId", "SequenceType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpSteps_FollowUpSequenceId_StepOrder",
                table: "FollowUpSteps",
                columns: new[] { "FollowUpSequenceId", "StepOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FollowUpExecutionLogs");

            migrationBuilder.DropTable(
                name: "FollowUpSteps");

            migrationBuilder.DropTable(
                name: "FollowUpRuns");

            migrationBuilder.DropTable(
                name: "FollowUpSequences");
        }
    }
}
