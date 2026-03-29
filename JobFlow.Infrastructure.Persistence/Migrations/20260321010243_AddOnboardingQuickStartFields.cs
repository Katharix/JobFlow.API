using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingQuickStartFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OnboardingPresetAppliedAt",
                table: "Organization",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnboardingPresetKey",
                table: "Organization",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnboardingTrack",
                table: "Organization",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OnboardingTrackSelectedAt",
                table: "Organization",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingPresetAppliedAt",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "OnboardingPresetKey",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "OnboardingTrack",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "OnboardingTrackSelectedAt",
                table: "Organization");
        }
    }
}
