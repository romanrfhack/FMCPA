using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track3DocumentAdministrativeHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HoldPlacedBy",
                table: "StoredDocuments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HoldPlacedUtc",
                table: "StoredDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoldReason",
                table: "StoredDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HoldReleasedUtc",
                table: "StoredDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrativeHold",
                table: "StoredDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_IsAdministrativeHold_RetentionReviewStatusCode_NextRetentionReviewUtc",
                table: "StoredDocuments",
                columns: new[] { "IsAdministrativeHold", "RetentionReviewStatusCode", "NextRetentionReviewUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_IsAdministrativeHold_RetentionReviewStatusCode_NextRetentionReviewUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "HoldPlacedBy",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "HoldPlacedUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "HoldReason",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "HoldReleasedUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "IsAdministrativeHold",
                table: "StoredDocuments");
        }
    }
}
