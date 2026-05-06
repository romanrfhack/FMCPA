using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track3DocumentRetentionBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetentionPolicyCode",
                table: "StoredDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RetentionUntilUtc",
                table: "StoredDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE StoredDocuments
                SET RetentionPolicyCode = CASE
                        WHEN DocumentClassCode = 'CERTIFICATE' THEN 'CERTIFICATE_REVIEW'
                        WHEN DocumentClassCode = 'SIGNED_DOCUMENT' THEN 'SIGNED_LONG_TERM'
                        WHEN DocumentClassCode IN ('SUPPORTING_DOCUMENT', 'PHOTO_EVIDENCE', 'VIDEO_EVIDENCE') THEN 'EVIDENCE_MEDIUM_TERM'
                        ELSE 'GENERIC_REVIEW'
                    END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE StoredDocuments
                SET RetentionUntilUtc = CASE
                        WHEN RetentionPolicyCode = 'SIGNED_LONG_TERM' THEN DATEADD(year, 7, CreatedUtc)
                        WHEN RetentionPolicyCode = 'EVIDENCE_MEDIUM_TERM' THEN DATEADD(year, 3, CreatedUtc)
                        ELSE DATEADD(year, 1, CreatedUtc)
                    END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "RetentionPolicyCode",
                table: "StoredDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "GENERIC_REVIEW",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RetentionUntilUtc",
                table: "StoredDocuments",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_RetentionPolicyCode_RetentionUntilUtc",
                table: "StoredDocuments",
                columns: new[] { "RetentionPolicyCode", "RetentionUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_RetentionPolicyCode_RetentionUntilUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "RetentionPolicyCode",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "RetentionUntilUtc",
                table: "StoredDocuments");
        }
    }
}
