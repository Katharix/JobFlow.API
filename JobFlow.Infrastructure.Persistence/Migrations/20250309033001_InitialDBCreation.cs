using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDBCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment");

            migrationBuilder.CreateTable(
                name: "JobStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TypeName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StripeCustomer",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Delinqent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeCustomer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Job",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    JobStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Job_JobStatus_JobStatusId",
                        column: x => x.JobStatusId,
                        principalTable: "JobStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeCustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StripeConnectedAccountId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasFreeAccount = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organization_OrganizationType_OrganizationTypeId",
                        column: x => x.OrganizationTypeId,
                        principalTable: "OrganizationType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Organization_StripeCustomer_StripeCustomerId",
                        column: x => x.StripeCustomerId,
                        principalSchema: "payment",
                        principalTable: "StripeCustomer",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrganizationClient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeCustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationClient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationClient_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationClient_StripeCustomer_StripeCustomerId",
                        column: x => x.StripeCustomerId,
                        principalSchema: "payment",
                        principalTable: "StripeCustomer",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrganizationService",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationService", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationService_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.InsertData(
                table: "JobStatus",
                columns: new[] { "Id", "Status" },
                values: new object[,]
                {
                    { new Guid("001a1323-185b-4624-987c-325b23bdb5c7"), "In Progress" },
                    { new Guid("0e48efd2-5783-4ea0-b091-6b4bf4377e38"), "On Hold" },
                    { new Guid("0e58e058-c9b8-4acf-b8ca-cb8d2eb857f4"), "Canceled" },
                    { new Guid("1ca909ea-e3e8-4364-a81f-7c5b93d9bc25"), "Awaiting Approval" },
                    { new Guid("5788453f-4c21-4c9b-b50c-f987c15d0cf2"), "Pending" },
                    { new Guid("878f4fa0-e7c4-4440-bf7e-e1bdf068b551"), "Completed" },
                    { new Guid("be7d4999-e30f-4d4a-8f8b-580344e14dcd"), "Failed" }
                });

            migrationBuilder.InsertData(
                table: "OrganizationType",
                columns: new[] { "Id", "TypeName" },
                values: new object[,]
                {
                    { new Guid("1921d982-22f8-4ed5-b4e3-fca82c5767eb"), "Cleaning Services" },
                    { new Guid("30530a32-a151-436d-a050-613eac4c22d5"), "IT & Network Installation" },
                    { new Guid("33341b2d-957f-4efb-94f7-3a015ae1a718"), "Car Detailing" },
                    { new Guid("37fc17e8-0a25-4119-9a71-7d160bb9c7b4"), "HVAC Services" },
                    { new Guid("393a5b3e-323e-4b76-aa86-0d4683ddcd49"), "Painting" },
                    { new Guid("408d2185-53b9-493d-8713-938114de90f5"), "Junk Removal" },
                    { new Guid("6ac2cabc-bbe3-4bc1-9879-5455de042cf4"), "Master Account" },
                    { new Guid("8f0d3e93-425b-4a53-b4d2-4c5eb97e490f"), "Carpentry and Woodworking" },
                    { new Guid("9362c957-0f41-4c20-9085-c01e449fdda2"), "Electrical Services" },
                    { new Guid("bf3b9512-8a9c-4a73-9f88-cb914c1573cd"), "Pest Control" },
                    { new Guid("bf489aa6-db19-42df-82bc-c116bd967e7e"), "General Contracting" },
                    { new Guid("e01750b0-0d01-4e25-abf7-6efa23509035"), "Plumbing" },
                    { new Guid("f64f078f-ecfb-4f3e-8640-236219fcf01e"), "Tree Removal" },
                    { new Guid("fbc6accf-0fb1-4908-b449-14f13b826f24"), "Landscaping and Gardening" }
                });

            migrationBuilder.InsertData(
                table: "Organization",
                columns: new[] { "Id", "Address1", "Address2", "City", "EmailAddress", "HasFreeAccount", "OrganizationName", "OrganizationTypeId", "PhoneNumber", "State", "StripeConnectedAccountId", "StripeCustomerId", "ZipCode" },
                values: new object[,]
                {
                    { new Guid("b3b20208-07ae-40a2-971e-adf3bb93fc8c"), "116 Terrill St", null, "Beckley", "vonbrown230@gmail.com", true, "Browns Cleaning Services", new Guid("1921d982-22f8-4ed5-b4e3-fca82c5767eb"), "304-731-1952", "WV", null, null, "25801" },
                    { new Guid("d464b178-a52d-440b-a064-42246f7e0756"), null, null, null, "jerry.daniel.phillips@gmail.com", true, "Katharix", new Guid("6ac2cabc-bbe3-4bc1-9879-5455de042cf4"), null, null, null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Job_JobStatusId",
                table: "Job",
                column: "JobStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_OrganizationTypeId",
                table: "Organization",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_StripeCustomerId",
                table: "Organization",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationClient_OrganizationId",
                table: "OrganizationClient",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationClient_StripeCustomerId",
                table: "OrganizationClient",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationClientJob_OrganizationClientId",
                table: "OrganizationClientJob",
                column: "OrganizationClientId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationService_OrganizationId",
                table: "OrganizationService",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationClientJob");

            migrationBuilder.DropTable(
                name: "OrganizationService");

            migrationBuilder.DropTable(
                name: "Job");

            migrationBuilder.DropTable(
                name: "OrganizationClient");

            migrationBuilder.DropTable(
                name: "JobStatus");

            migrationBuilder.DropTable(
                name: "Organization");

            migrationBuilder.DropTable(
                name: "OrganizationType");

            migrationBuilder.DropTable(
                name: "StripeCustomer",
                schema: "payment");
        }
    }
}
