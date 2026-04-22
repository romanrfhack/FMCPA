using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage06FederationAndCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FederationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CounterpartyOrInstitution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    StatusCatalogEntryId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederationActions_ModuleStatusCatalog_StatusCatalogEntryId",
                        column: x => x.StatusCatalogEntryId,
                        principalTable: "ModuleStatusCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FederationDonations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DonationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DonationType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StatusCatalogEntryId = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationDonations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederationDonations_ModuleStatusCatalog_StatusCatalogEntryId",
                        column: x => x.StatusCatalogEntryId,
                        principalTable: "ModuleStatusCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FederationActionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FederationActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantSide = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParticipantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrganizationOrDependency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationActionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederationActionParticipants_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FederationActionParticipants_FederationActions_FederationActionId",
                        column: x => x.FederationActionId,
                        principalTable: "FederationActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FederationDonationApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FederationDonationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeneficiaryOrDestinationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApplicationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StatusCatalogEntryId = table.Column<int>(type: "int", nullable: false),
                    VerificationDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClosingDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationDonationApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederationDonationApplications_FederationDonations_FederationDonationId",
                        column: x => x.FederationDonationId,
                        principalTable: "FederationDonations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FederationDonationApplications_ModuleStatusCatalog_StatusCatalogEntryId",
                        column: x => x.StatusCatalogEntryId,
                        principalTable: "ModuleStatusCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FederationDonationApplicationCommissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FederationDonationApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommissionTypeId = table.Column<int>(type: "int", nullable: false),
                    RecipientCategory = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RecipientContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationDonationApplicationCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederationDonationApplicationCommissions_CommissionTypes_CommissionTypeId",
                        column: x => x.CommissionTypeId,
                        principalTable: "CommissionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FederationDonationApplicationCommissions_Contacts_RecipientContactId",
                        column: x => x.RecipientContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FederationDonationApplicationCommissions_FederationDonationApplications_FederationDonationApplicationId",
                        column: x => x.FederationDonationApplicationId,
                        principalTable: "FederationDonationApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FederationDonationApplicationEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FederationDonationApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_FederationDonationApplicationEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederationDonationApplicationEvidences_EvidenceTypes_EvidenceTypeId",
                        column: x => x.EvidenceTypeId,
                        principalTable: "EvidenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FederationDonationApplicationEvidences_FederationDonationApplications_FederationDonationApplicationId",
                        column: x => x.FederationDonationApplicationId,
                        principalTable: "FederationDonationApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ModuleStatusCatalog",
                columns: new[] { "Id", "AlertsEnabledByDefault", "ContextCode", "ContextName", "Description", "IsActive", "IsClosed", "ModuleCode", "ModuleName", "SortOrder", "StatusCode", "StatusName" },
                values: new object[,]
                {
                    { 1501, true, "FEDERATION_ACTION", "Gestion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 1, "IN_PROCESS", "En proceso" },
                    { 1502, true, "FEDERATION_ACTION", "Gestion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 2, "FOLLOW_UP_PENDING", "Seguimiento pendiente" },
                    { 1503, false, "FEDERATION_ACTION", "Gestion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 3, "CONCLUDED", "Concluido" },
                    { 1504, false, "FEDERATION_ACTION", "Gestion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "FEDERATION", "Federacion", 4, "CLOSED", "Cerrado" },
                    { 1601, true, "FEDERATION_DONATION", "Donacion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 1, "NOT_APPLIED", "No aplicada" },
                    { 1602, true, "FEDERATION_DONATION", "Donacion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 2, "PARTIALLY_APPLIED", "Aplicacion parcial" },
                    { 1603, false, "FEDERATION_DONATION", "Donacion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 3, "APPLIED", "Aplicada" },
                    { 1604, false, "FEDERATION_DONATION", "Donacion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "FEDERATION", "Federacion", 4, "CLOSED", "Cerrada" },
                    { 1701, true, "FEDERATION_DONATION_APPLICATION", "Aplicacion de donacion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 1, "PARTIALLY_APPLIED", "Aplicacion parcial" },
                    { 1702, false, "FEDERATION_DONATION_APPLICATION", "Aplicacion de donacion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 2, "APPLIED", "Aplicada" },
                    { 1703, false, "FEDERATION_DONATION_APPLICATION", "Aplicacion de donacion de federacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "FEDERATION", "Federacion", 3, "CLOSED", "Cerrada" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FederationActionParticipants_ContactId",
                table: "FederationActionParticipants",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationActionParticipants_FederationActionId",
                table: "FederationActionParticipants",
                column: "FederationActionId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationActionParticipants_ParticipantSide",
                table: "FederationActionParticipants",
                column: "ParticipantSide");

            migrationBuilder.CreateIndex(
                name: "IX_FederationActions_ActionDate",
                table: "FederationActions",
                column: "ActionDate");

            migrationBuilder.CreateIndex(
                name: "IX_FederationActions_ActionTypeCode",
                table: "FederationActions",
                column: "ActionTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_FederationActions_StatusCatalogEntryId",
                table: "FederationActions",
                column: "StatusCatalogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplicationCommissions_CommissionTypeId",
                table: "FederationDonationApplicationCommissions",
                column: "CommissionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplicationCommissions_FederationDonationApplicationId",
                table: "FederationDonationApplicationCommissions",
                column: "FederationDonationApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplicationCommissions_RecipientCategory",
                table: "FederationDonationApplicationCommissions",
                column: "RecipientCategory");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplicationCommissions_RecipientContactId",
                table: "FederationDonationApplicationCommissions",
                column: "RecipientContactId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplicationEvidences_EvidenceTypeId",
                table: "FederationDonationApplicationEvidences",
                column: "EvidenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplicationEvidences_FederationDonationApplicationId",
                table: "FederationDonationApplicationEvidences",
                column: "FederationDonationApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplications_ApplicationDate",
                table: "FederationDonationApplications",
                column: "ApplicationDate");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplications_FederationDonationId",
                table: "FederationDonationApplications",
                column: "FederationDonationId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonationApplications_StatusCatalogEntryId",
                table: "FederationDonationApplications",
                column: "StatusCatalogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonations_DonationDate",
                table: "FederationDonations",
                column: "DonationDate");

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonations_Reference",
                table: "FederationDonations",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FederationDonations_StatusCatalogEntryId",
                table: "FederationDonations",
                column: "StatusCatalogEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FederationActionParticipants");

            migrationBuilder.DropTable(
                name: "FederationDonationApplicationCommissions");

            migrationBuilder.DropTable(
                name: "FederationDonationApplicationEvidences");

            migrationBuilder.DropTable(
                name: "FederationActions");

            migrationBuilder.DropTable(
                name: "FederationDonationApplications");

            migrationBuilder.DropTable(
                name: "FederationDonations");

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1501);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1502);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1503);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1504);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1601);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1602);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1603);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1604);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1701);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1702);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1703);
        }
    }
}
