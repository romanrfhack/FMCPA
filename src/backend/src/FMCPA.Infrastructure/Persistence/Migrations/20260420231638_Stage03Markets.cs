using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage03Markets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModuleStatusCatalog_ModuleCode_StatusCode",
                table: "ModuleStatusCatalog");

            migrationBuilder.AddColumn<string>(
                name: "ContextCode",
                table: "ModuleStatusCatalog",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContextName",
                table: "ModuleStatusCatalog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Markets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Borough = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StatusCatalogEntryId = table.Column<int>(type: "int", nullable: false),
                    SecretaryGeneralContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SecretaryGeneralName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Markets_Contacts_SecretaryGeneralContactId",
                        column: x => x.SecretaryGeneralContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Markets_ModuleStatusCatalog_StatusCatalogEntryId",
                        column: x => x.StatusCatalogEntryId,
                        principalTable: "ModuleStatusCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AdvanceSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StatusCatalogEntryId = table.Column<int>(type: "int", nullable: false),
                    FollowUpOrResolution = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    FinalSatisfaction = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketIssues_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketIssues_ModuleStatusCatalog_StatusCatalogEntryId",
                        column: x => x.StatusCatalogEntryId,
                        principalTable: "ModuleStatusCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketTenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CertificateValidityTo = table.Column<DateOnly>(type: "date", nullable: false),
                    BusinessLine = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MobilePhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WhatsAppPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    CertificateOriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    CertificateStoredRelativePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CertificateContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CertificateFileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CertificateUploadedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketTenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketTenants_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketTenants_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.UpdateData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "ContextCode", "ContextName" },
                values: new object[] { "GENERAL", "General" });

            migrationBuilder.InsertData(
                table: "ModuleStatusCatalog",
                columns: new[] { "Id", "AlertsEnabledByDefault", "ContextCode", "ContextName", "Description", "IsActive", "IsClosed", "ModuleCode", "ModuleName", "SortOrder", "StatusCode", "StatusName" },
                values: new object[,]
                {
                    { 1001, true, "MARKET", "Mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "MARKETS", "Mercados", 1, "ACTIVE", "Activo" },
                    { 1002, true, "MARKET", "Mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "MARKETS", "Mercados", 2, "INACTIVE", "Inactivo" },
                    { 1003, false, "MARKET", "Mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "MARKETS", "Mercados", 3, "CLOSED", "Cerrado" },
                    { 1004, false, "MARKET", "Mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "MARKETS", "Mercados", 4, "ARCHIVED", "Archivado" },
                    { 1101, true, "MARKET_ISSUE", "Incidencia de mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "MARKETS", "Mercados", 1, "PROCEEDED", "Procedio" },
                    { 1102, true, "MARKET_ISSUE", "Incidencia de mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "MARKETS", "Mercados", 2, "IN_ATTENTION", "En atencion" },
                    { 1103, true, "MARKET_ISSUE", "Incidencia de mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "MARKETS", "Mercados", 3, "NOT_ATTENDED", "No atendido" },
                    { 1104, false, "MARKET_ISSUE", "Incidencia de mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "MARKETS", "Mercados", 4, "ATTENDED_SATISFACTORILY", "Atendido satisfactoriamente" },
                    { 1105, false, "MARKET_ISSUE", "Incidencia de mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "MARKETS", "Mercados", 5, "CONCLUDED_UNSATISFACTORILY", "Concluido no satisfactorio" },
                    { 1106, false, "MARKET_ISSUE", "Incidencia de mercado", "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "MARKETS", "Mercados", 6, "CLOSED", "Cerrado" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleStatusCatalog_ModuleCode_ContextCode_StatusCode",
                table: "ModuleStatusCatalog",
                columns: new[] { "ModuleCode", "ContextCode", "StatusCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketIssues_IssueDate",
                table: "MarketIssues",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_MarketIssues_MarketId",
                table: "MarketIssues",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketIssues_StatusCatalogEntryId",
                table: "MarketIssues",
                column: "StatusCatalogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Markets_Name",
                table: "Markets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Markets_SecretaryGeneralContactId",
                table: "Markets",
                column: "SecretaryGeneralContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Markets_StatusCatalogEntryId",
                table: "Markets",
                column: "StatusCatalogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTenants_CertificateValidityTo",
                table: "MarketTenants",
                column: "CertificateValidityTo");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTenants_ContactId",
                table: "MarketTenants",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTenants_MarketId",
                table: "MarketTenants",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTenants_MarketId_CertificateValidityTo",
                table: "MarketTenants",
                columns: new[] { "MarketId", "CertificateValidityTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketIssues");

            migrationBuilder.DropTable(
                name: "MarketTenants");

            migrationBuilder.DropTable(
                name: "Markets");

            migrationBuilder.DropIndex(
                name: "IX_ModuleStatusCatalog_ModuleCode_ContextCode_StatusCode",
                table: "ModuleStatusCatalog");

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1101);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1102);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1103);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1104);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1105);

            migrationBuilder.DeleteData(
                table: "ModuleStatusCatalog",
                keyColumn: "Id",
                keyValue: 1106);

            migrationBuilder.DropColumn(
                name: "ContextCode",
                table: "ModuleStatusCatalog");

            migrationBuilder.DropColumn(
                name: "ContextName",
                table: "ModuleStatusCatalog");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleStatusCatalog_ModuleCode_StatusCode",
                table: "ModuleStatusCatalog",
                columns: new[] { "ModuleCode", "StatusCode" },
                unique: true);
        }
    }
}
