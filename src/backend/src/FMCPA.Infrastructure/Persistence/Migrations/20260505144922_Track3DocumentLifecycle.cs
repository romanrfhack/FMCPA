using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track3DocumentLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "StoredDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedUtc",
                table: "StoredDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusCode",
                table: "StoredDocuments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_StatusCode_CreatedUtc",
                table: "StoredDocuments",
                columns: new[] { "StatusCode", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_StatusCode_CreatedUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "ArchivedUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "StoredDocuments");
        }
    }
}
