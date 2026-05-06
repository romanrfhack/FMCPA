using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track3RetentionOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetentionOverridePolicyCode",
                table: "StoredDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetentionOverrideReason",
                table: "StoredDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RetentionOverrideUntilUtc",
                table: "StoredDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_RetentionOverridePolicyCode_RetentionOverrideUntilUtc",
                table: "StoredDocuments",
                columns: new[] { "RetentionOverridePolicyCode", "RetentionOverrideUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_RetentionOverridePolicyCode_RetentionOverrideUntilUtc",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "RetentionOverridePolicyCode",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "RetentionOverrideReason",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "RetentionOverrideUntilUtc",
                table: "StoredDocuments");
        }
    }
}
