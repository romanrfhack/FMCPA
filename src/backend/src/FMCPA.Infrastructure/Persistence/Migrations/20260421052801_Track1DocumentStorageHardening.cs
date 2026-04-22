using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track1DocumentStorageHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DocumentAreaCode = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredRelativePath = table.Column<string>(type: "nvarchar(520)", maxLength: 520, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Sha256Hex = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsLegacyBackfill = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_CreatedUtc",
                table: "StoredDocuments",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_DocumentAreaCode_EntityType_EntityId",
                table: "StoredDocuments",
                columns: new[] { "DocumentAreaCode", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_ModuleCode_EntityType_EntityId",
                table: "StoredDocuments",
                columns: new[] { "ModuleCode", "EntityType", "EntityId" });

            migrationBuilder.Sql(
                """
                INSERT INTO StoredDocuments (
                    Id,
                    ModuleCode,
                    DocumentAreaCode,
                    EntityType,
                    EntityId,
                    OriginalFileName,
                    StoredRelativePath,
                    ContentType,
                    SizeBytes,
                    CreatedUtc,
                    Sha256Hex,
                    IsLegacyBackfill)
                SELECT
                    NEWID(),
                    'MARKETS',
                    'MARKETS_TENANT_CERTIFICATES',
                    'MARKET_TENANT',
                    Id,
                    CertificateOriginalFileName,
                    CertificateStoredRelativePath,
                    COALESCE(NULLIF(CertificateContentType, ''), 'application/octet-stream'),
                    CertificateFileSizeBytes,
                    CertificateUploadedUtc,
                    NULL,
                    CAST(1 AS bit)
                FROM MarketTenants
                WHERE CertificateStoredRelativePath IS NOT NULL
                    AND CertificateStoredRelativePath <> '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO StoredDocuments (
                    Id,
                    ModuleCode,
                    DocumentAreaCode,
                    EntityType,
                    EntityId,
                    OriginalFileName,
                    StoredRelativePath,
                    ContentType,
                    SizeBytes,
                    CreatedUtc,
                    Sha256Hex,
                    IsLegacyBackfill)
                SELECT
                    NEWID(),
                    'DONATARIAS',
                    'DONATIONS_APPLICATION_EVIDENCES',
                    'DONATION_APPLICATION_EVIDENCE',
                    Id,
                    OriginalFileName,
                    StoredRelativePath,
                    COALESCE(NULLIF(ContentType, ''), 'application/octet-stream'),
                    FileSizeBytes,
                    UploadedUtc,
                    NULL,
                    CAST(1 AS bit)
                FROM DonationApplicationEvidences
                WHERE StoredRelativePath IS NOT NULL
                    AND StoredRelativePath <> '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO StoredDocuments (
                    Id,
                    ModuleCode,
                    DocumentAreaCode,
                    EntityType,
                    EntityId,
                    OriginalFileName,
                    StoredRelativePath,
                    ContentType,
                    SizeBytes,
                    CreatedUtc,
                    Sha256Hex,
                    IsLegacyBackfill)
                SELECT
                    NEWID(),
                    'FEDERATION',
                    'FEDERATION_APPLICATION_EVIDENCES',
                    'FEDERATION_DONATION_APPLICATION_EVIDENCE',
                    Id,
                    OriginalFileName,
                    StoredRelativePath,
                    COALESCE(NULLIF(ContentType, ''), 'application/octet-stream'),
                    FileSizeBytes,
                    UploadedUtc,
                    NULL,
                    CAST(1 AS bit)
                FROM FederationDonationApplicationEvidences
                WHERE StoredRelativePath IS NOT NULL
                    AND StoredRelativePath <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredDocuments");
        }
    }
}
