using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track3DocumentRetentionReviewQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRetentionReviewUtc",
                table: "StoredDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRetentionReviewUtc",
                table: "StoredDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetentionReviewNotes",
                table: "StoredDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetentionReviewStatusCode",
                table: "StoredDocuments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "REVIEW_PENDING");

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_RetentionReviewStatusCode_NextRetentionReviewUtc",
                table: "StoredDocuments",
                columns: new[] { "RetentionReviewStatusCode", "NextRetentionReviewUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_RetentionReviewStatusCode_NextRetentionReviewUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "LastRetentionReviewUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "NextRetentionReviewUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "RetentionReviewNotes",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "RetentionReviewStatusCode",
                table: "StoredDocuments");
        }
    }
}
