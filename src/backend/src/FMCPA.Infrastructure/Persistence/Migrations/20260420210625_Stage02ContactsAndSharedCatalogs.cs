using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage02ContactsAndSharedCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommissionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleStatusCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatusName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    AlertsEnabledByDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleStatusCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactTypeId = table.Column<int>(type: "int", nullable: false),
                    OrganizationOrDependency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoleTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MobilePhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WhatsAppPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_ContactTypes_ContactTypeId",
                        column: x => x.ContactTypeId,
                        principalTable: "ContactTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContactParticipations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContextType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContextKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactParticipations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactParticipations_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CommissionTypes",
                columns: new[] { "Id", "Code", "Description", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "ADMINISTRATION", "Tipo de comision base compartido.", true, "Administracion", 1 },
                    { 2, "OPERATIONAL", "Tipo de comision base compartido.", true, "Operativa", 2 },
                    { 3, "COMMERCIAL", "Tipo de comision base compartido.", true, "Comercial", 3 },
                    { 4, "INTERMEDIATION", "Tipo de comision base compartido.", true, "Intermediacion", 4 },
                    { 5, "EXTERNAL_MANAGEMENT", "Tipo de comision base compartido.", true, "Gestion externa", 5 },
                    { 6, "INSTITUTIONAL_LINK", "Tipo de comision base compartido.", true, "Vinculacion institucional", 6 },
                    { 7, "PROMOTER", "Tipo de comision base compartido.", true, "Promotor", 7 },
                    { 8, "COORDINATION", "Tipo de comision base compartido.", true, "Coordinacion", 8 },
                    { 9, "SPECIAL_NEGOTIATED", "Tipo de comision base compartido.", true, "Especial/negociada", 9 }
                });

            migrationBuilder.InsertData(
                table: "ContactTypes",
                columns: new[] { "Id", "Code", "Description", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "INTERNAL", "Contacto interno reutilizable dentro de la operacion del proyecto.", true, "Interno", 1 },
                    { 2, "EXTERNAL", "Contacto externo reutilizable entre proyectos y gestiones.", true, "Externo", 2 }
                });

            migrationBuilder.InsertData(
                table: "EvidenceTypes",
                columns: new[] { "Id", "Code", "Description", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "PHOTO", "Evidencia grafica basica.", true, "Fotografia", 1 },
                    { 2, "VIDEO", "Evidencia audiovisual.", true, "Video", 2 },
                    { 3, "SIGNED_DOCUMENT", "Documento con firma como soporte.", true, "Documento firmado", 3 },
                    { 4, "SUPPORT_DOCUMENT", "Documento de respaldo general.", true, "Documento soporte", 4 },
                    { 5, "OTHER", "Categoria abierta controlada para nueva evidencia.", true, "Otro", 5 }
                });

            migrationBuilder.InsertData(
                table: "ModuleStatusCatalog",
                columns: new[] { "Id", "AlertsEnabledByDefault", "Description", "IsActive", "IsClosed", "ModuleCode", "ModuleName", "SortOrder", "StatusCode", "StatusName" },
                values: new object[,]
                {
                    { 1, true, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "MARKETS", "Mercados", 1, "ACTIVE", "Activo" },
                    { 2, false, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "MARKETS", "Mercados", 2, "CLOSED", "Cerrado" },
                    { 3, true, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "DONATARIAS", "Donatarias", 1, "ACTIVE", "Activo" },
                    { 4, false, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "DONATARIAS", "Donatarias", 2, "CLOSED", "Cerrado" },
                    { 5, true, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FINANCIALS", "Financieras", 1, "ACTIVE", "Activo" },
                    { 6, false, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "FINANCIALS", "Financieras", 2, "CLOSED", "Cerrado" },
                    { 7, true, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "FEDERATION", "Federacion", 1, "ACTIVE", "Activo" },
                    { 8, false, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "FEDERATION", "Federacion", 2, "CLOSED", "Cerrado" },
                    { 9, true, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "COMMISSIONS", "Comisiones", 1, "ACTIVE", "Activo" },
                    { 10, false, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "COMMISSIONS", "Comisiones", 2, "CLOSED", "Cerrado" },
                    { 11, true, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "CONTACTS", "Contactos", 1, "ACTIVE", "Activo" },
                    { 12, false, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "CONTACTS", "Contactos", 2, "CLOSED", "Cerrado" },
                    { 13, true, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, false, "BITACORA", "Bitacora", 1, "ACTIVE", "Activo" },
                    { 14, false, "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.", true, true, "BITACORA", "Bitacora", 2, "CLOSED", "Cerrado" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTypes_Code",
                table: "CommissionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactParticipations_ContactId_ModuleCode_ContextType_ContextKey",
                table: "ContactParticipations",
                columns: new[] { "ContactId", "ModuleCode", "ContextType", "ContextKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_ContactTypeId",
                table: "Contacts",
                column: "ContactTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactTypes_Code",
                table: "ContactTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceTypes_Code",
                table: "EvidenceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleStatusCatalog_ModuleCode_StatusCode",
                table: "ModuleStatusCatalog",
                columns: new[] { "ModuleCode", "StatusCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionTypes");

            migrationBuilder.DropTable(
                name: "ContactParticipations");

            migrationBuilder.DropTable(
                name: "EvidenceTypes");

            migrationBuilder.DropTable(
                name: "ModuleStatusCatalog");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "ContactTypes");
        }
    }
}
