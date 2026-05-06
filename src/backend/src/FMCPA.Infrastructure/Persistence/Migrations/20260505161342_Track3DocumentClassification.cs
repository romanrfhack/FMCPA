using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track3DocumentClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessPurpose",
                table: "StoredDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationNotes",
                table: "StoredDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentClassCode",
                table: "StoredDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "OTHER");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimaryDocument",
                table: "StoredDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE StoredDocuments
                SET DocumentClassCode = 'CERTIFICATE',
                    BusinessPurpose = 'Acreditar la cedula digitalizada del locatario.',
                    IsPrimaryDocument = 1
                WHERE DocumentAreaCode = 'MARKETS_TENANT_CERTIFICATES';
                """);

            migrationBuilder.Sql(
                """
                UPDATE StoredDocuments
                SET DocumentClassCode = CASE
                        WHEN LOWER(ContentType) IN ('image/jpeg', 'image/png') THEN 'PHOTO_EVIDENCE'
                        ELSE 'SUPPORTING_DOCUMENT'
                    END,
                    BusinessPurpose = 'Soportar la aplicacion documental de una donacion.',
                    IsPrimaryDocument = 0
                WHERE DocumentAreaCode = 'DONATIONS_APPLICATION_EVIDENCES';
                """);

            migrationBuilder.Sql(
                """
                UPDATE StoredDocuments
                SET DocumentClassCode = CASE
                        WHEN LOWER(ContentType) IN ('image/jpeg', 'image/png') THEN 'PHOTO_EVIDENCE'
                        ELSE 'SUPPORTING_DOCUMENT'
                    END,
                    BusinessPurpose = 'Soportar la aplicacion documental de Federacion.',
                    IsPrimaryDocument = 0
                WHERE DocumentAreaCode = 'FEDERATION_APPLICATION_EVIDENCES';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_DocumentClassCode_CreatedUtc",
                table: "StoredDocuments",
                columns: new[] { "DocumentClassCode", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_DocumentClassCode_CreatedUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "BusinessPurpose",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "ClassificationNotes",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentClassCode",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "IsPrimaryDocument",
                table: "StoredDocuments");
        }
    }
}
