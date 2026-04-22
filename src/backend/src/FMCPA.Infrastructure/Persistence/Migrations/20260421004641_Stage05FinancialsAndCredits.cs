using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage05FinancialsAndCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialPermits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstitutionOrDependency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PlaceOrStand = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NegotiatedTerms = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StatusCatalogEntryId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialPermits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialPermits_ModuleStatusCatalog_StatusCatalogEntryId",
                        column: x => x.StatusCatalogEntryId,
                        principalTable: "ModuleStatusCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialCredits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialPermitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromoterContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromoterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BeneficiaryContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeneficiaryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WhatsAppPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AuthorizationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialCredits_Contacts_BeneficiaryContactId",
                        column: x => x.BeneficiaryContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialCredits_Contacts_PromoterContactId",
                        column: x => x.PromoterContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialCredits_FinancialPermits_FinancialPermitId",
                        column: x => x.FinancialPermitId,
                        principalTable: "FinancialPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialCreditCommissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialCreditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_FinancialCreditCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialCreditCommissions_CommissionTypes_CommissionTypeId",
                        column: x => x.CommissionTypeId,
                        principalTable: "CommissionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialCreditCommissions_Contacts_RecipientContactId",
                        column: x => x.RecipientContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialCreditCommissions_FinancialCredits_FinancialCreditId",
                        column: x => x.FinancialCreditId,
                        principalTable: "FinancialCredits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ModuleStatusCatalog",
                columns: new[] { "Id", "AlertsEnabledByDefault", "ContextCode", "ContextName", "Description", "IsActive", "IsClosed", "ModuleCode", "ModuleName", "SortOrder", "StatusCode", "StatusName" },
                values: new object[,]
                {
                    { 1401, true, "FINANCIAL_PERMIT", "Oficio o autorizacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FINANCIALS", "Financieras", 1, "ACCEPTED", "Aceptado" },
                    { 1402, false, "FINANCIAL_PERMIT", "Oficio o autorizacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "FINANCIALS", "Financieras", 2, "REJECTED", "Rechazado" },
                    { 1403, true, "FINANCIAL_PERMIT", "Oficio o autorizacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FINANCIALS", "Financieras", 3, "IN_PROCESS", "En proceso" },
                    { 1404, true, "FINANCIAL_PERMIT", "Oficio o autorizacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FINANCIALS", "Financieras", 4, "RENEW", "Renovar" },
                    { 1405, false, "FINANCIAL_PERMIT", "Oficio o autorizacion", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "FINANCIALS", "Financieras", 5, "CLOSED", "Cerrado" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCreditCommissions_CommissionTypeId",
                table: "FinancialCreditCommissions",
                column: "CommissionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCreditCommissions_FinancialCreditId",
                table: "FinancialCreditCommissions",
                column: "FinancialCreditId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCreditCommissions_RecipientCategory",
                table: "FinancialCreditCommissions",
                column: "RecipientCategory");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCreditCommissions_RecipientContactId",
                table: "FinancialCreditCommissions",
                column: "RecipientContactId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCredits_AuthorizationDate",
                table: "FinancialCredits",
                column: "AuthorizationDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCredits_BeneficiaryContactId",
                table: "FinancialCredits",
                column: "BeneficiaryContactId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCredits_FinancialPermitId",
                table: "FinancialCredits",
                column: "FinancialPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCredits_FinancialPermitId_AuthorizationDate",
                table: "FinancialCredits",
                columns: new[] { "FinancialPermitId", "AuthorizationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCredits_PromoterContactId",
                table: "FinancialCredits",
                column: "PromoterContactId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPermits_StatusCatalogEntryId",
                table: "FinancialPermits",
                column: "StatusCatalogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPermits_ValidTo",
                table: "FinancialPermits",
                column: "ValidTo");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPermits_ValidTo_StatusCatalogEntryId",
                table: "FinancialPermits",
                columns: new[] { "ValidTo", "StatusCatalogEntryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialCreditCommissions");

            migrationBuilder.DropTable(
                name: "FinancialCredits");

            migrationBuilder.DropTable(
                name: "FinancialPermits");

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1401);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1402);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1403);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1404);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1405);
        }
    }
}
