using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionRecordTBL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionRecord",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderSubscriptionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderPriceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CanceledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionRecord_CustomerPaymentProfile_PaymentProfileId",
                        column: x => x.PaymentProfileId,
                        principalSchema: "payment",
                        principalTable: "CustomerPaymentProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRecord_PaymentProfileId",
                schema: "payment",
                table: "SubscriptionRecord",
                column: "PaymentProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionRecord",
                schema: "payment");
        }
    }
}
