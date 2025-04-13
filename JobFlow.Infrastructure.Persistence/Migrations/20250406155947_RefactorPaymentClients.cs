using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPaymentClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organization_StripeCustomer_StripeCustomerId",
                table: "Organization");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationClient_StripeCustomer_StripeCustomerId",
                table: "OrganizationClient");

            migrationBuilder.DropTable(
                name: "StripeCustomer",
                schema: "payment");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationClient_StripeCustomerId",
                table: "OrganizationClient");

            migrationBuilder.DropIndex(
                name: "IX_Organization_StripeCustomerId",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "OrganizationClient");

            migrationBuilder.DropColumn(
                name: "StripeConnectedAccountId",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Organization");

            migrationBuilder.AddColumn<int>(
                name: "PaymentProvider",
                table: "Organization",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CustomerPaymentProfile",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ProviderCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultPaymentMethodId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelinquent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganizationClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPaymentProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPaymentProfile_OrganizationClient_OrganizationClientId",
                        column: x => x.OrganizationClientId,
                        principalTable: "OrganizationClient",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerPaymentProfile_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Organization",
                keyColumn: "Id",
                keyValue: new Guid("b3b20208-07ae-40a2-971e-adf3bb93fc8c"),
                column: "PaymentProvider",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Organization",
                keyColumn: "Id",
                keyValue: new Guid("d464b178-a52d-440b-a064-42246f7e0756"),
                column: "PaymentProvider",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPaymentProfile_OrganizationClientId",
                schema: "payment",
                table: "CustomerPaymentProfile",
                column: "OrganizationClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPaymentProfile_OrganizationId",
                schema: "payment",
                table: "CustomerPaymentProfile",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPaymentProfile",
                schema: "payment");

            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                table: "Organization");

            migrationBuilder.AddColumn<Guid>(
                name: "StripeCustomerId",
                table: "OrganizationClient",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectedAccountId",
                table: "Organization",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StripeCustomerId",
                table: "Organization",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StripeCustomer",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Delinqent = table.Column<bool>(type: "bit", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeCustomer", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Organization",
                keyColumn: "Id",
                keyValue: new Guid("b3b20208-07ae-40a2-971e-adf3bb93fc8c"),
                columns: new[] { "StripeConnectedAccountId", "StripeCustomerId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Organization",
                keyColumn: "Id",
                keyValue: new Guid("d464b178-a52d-440b-a064-42246f7e0756"),
                columns: new[] { "StripeConnectedAccountId", "StripeCustomerId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationClient_StripeCustomerId",
                table: "OrganizationClient",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_StripeCustomerId",
                table: "Organization",
                column: "StripeCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organization_StripeCustomer_StripeCustomerId",
                table: "Organization",
                column: "StripeCustomerId",
                principalSchema: "payment",
                principalTable: "StripeCustomer",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationClient_StripeCustomer_StripeCustomerId",
                table: "OrganizationClient",
                column: "StripeCustomerId",
                principalSchema: "payment",
                principalTable: "StripeCustomer",
                principalColumn: "Id");
        }
    }
}
