using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatExternalSenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                schema: "messaging",
                table: "Message",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ExternalSenderName",
                schema: "messaging",
                table: "Message",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSenderPhone",
                schema: "messaging",
                table: "Message",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSenderType",
                schema: "messaging",
                table: "Message",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationClientId",
                schema: "messaging",
                table: "Conversation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_OrganizationClientId",
                schema: "messaging",
                table: "Conversation",
                column: "OrganizationClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversation_OrganizationClient_OrganizationClientId",
                schema: "messaging",
                table: "Conversation",
                column: "OrganizationClientId",
                principalTable: "OrganizationClient",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversation_OrganizationClient_OrganizationClientId",
                schema: "messaging",
                table: "Conversation");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_OrganizationClientId",
                schema: "messaging",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "ExternalSenderName",
                schema: "messaging",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "ExternalSenderPhone",
                schema: "messaging",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "ExternalSenderType",
                schema: "messaging",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "OrganizationClientId",
                schema: "messaging",
                table: "Conversation");

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                schema: "messaging",
                table: "Message",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
