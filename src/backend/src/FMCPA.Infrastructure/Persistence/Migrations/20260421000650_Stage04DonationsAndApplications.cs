using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage04DonationsAndApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Donations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonorEntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DonationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DonationType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    StatusCatalogEntryId = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Donations_ModuleStatusCatalog_StatusCatalogEntryId",
                        column: x => x.StatusCatalogEntryId,
                        principalTable: "ModuleStatusCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonationApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeneficiaryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResponsibleContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApplicationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StatusCatalogEntryId = table.Column<int>(type: "int", nullable: false),
                    VerificationDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClosingDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationApplications_Contacts_ResponsibleContactId",
                        column: x => x.ResponsibleContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonationApplications_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonationApplications_ModuleStatusCatalog_StatusCatalogEntryId",
                        column: x => x.StatusCatalogEntryId,
                        principalTable: "ModuleStatusCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonationApplicationEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonationApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceTypeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredRelativePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationApplicationEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationApplicationEvidences_DonationApplications_DonationApplicationId",
                        column: x => x.DonationApplicationId,
                        principalTable: "DonationApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonationApplicationEvidences_EvidenceTypes_EvidenceTypeId",
                        column: x => x.EvidenceTypeId,
                        principalTable: "EvidenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ModuleStatusCatalog",
                columns: new[] { "Id", "AlertsEnabledByDefault", "ContextCode", "ContextName", "Description", "IsActive", "IsClosed", "ModuleCode", "ModuleName", "SortOrder", "StatusCode", "StatusName" },
                values: new object[,]
                {
                    { 1201, true, "DONATION", "Donacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "DONATARIAS", "Donatarias", 1, "NOT_APPLIED", "No aplicada" },
                    { 1202, true, "DONATION", "Donacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "DONATARIAS", "Donatarias", 2, "PARTIALLY_APPLIED", "Aplicacion parcial" },
                    { 1203, false, "DONATION", "Donacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "DONATARIAS", "Donatarias", 3, "APPLIED", "Aplicada" },
                    { 1204, false, "DONATION", "Donacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "DONATARIAS", "Donatarias", 4, "CLOSED", "Cerrada" },
                    { 1301, true, "DONATION_APPLICATION", "Aplicacion de donacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "DONATARIAS", "Donatarias", 1, "PARTIALLY_APPLIED", "Aplicacion parcial" },
                    { 1302, false, "DONATION_APPLICATION", "Aplicacion de donacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "DONATARIAS", "Donatarias", 2, "APPLIED", "Aplicada" },
                    { 1303, false, "DONATION_APPLICATION", "Aplicacion de donacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "DONATARIAS", "Donatarias", 3, "CLOSED", "Cerrada" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonationApplicationEvidences_DonationApplicationId",
                table: "DonationApplicationEvidences",
                column: "DonationApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationApplicationEvidences_EvidenceTypeId",
                table: "DonationApplicationEvidences",
                column: "EvidenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationApplications_ApplicationDate",
                table: "DonationApplications",
                column: "ApplicationDate");

            migrationBuilder.CreateIndex(
                name: "IX_DonationApplications_DonationId",
                table: "DonationApplications",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationApplications_DonationId_ApplicationDate",
                table: "DonationApplications",
                columns: new[] { "DonationId", "ApplicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DonationApplications_ResponsibleContactId",
                table: "DonationApplications",
                column: "ResponsibleContactId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationApplications_StatusCatalogEntryId",
                table: "DonationApplications",
                column: "StatusCatalogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_DonationDate",
                table: "Donations",
                column: "DonationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_DonationDate_StatusCatalogEntryId",
                table: "Donations",
                columns: new[] { "DonationDate", "StatusCatalogEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Donations_StatusCatalogEntryId",
                table: "Donations",
                column: "StatusCatalogEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonationApplicationEvidences");

            migrationBuilder.DropTable(
                name: "DonationApplications");

            migrationBuilder.DropTable(
                name: "Donations");

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1201);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1202);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1203);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1204);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1301);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1302);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1303);
        }
    }
}
