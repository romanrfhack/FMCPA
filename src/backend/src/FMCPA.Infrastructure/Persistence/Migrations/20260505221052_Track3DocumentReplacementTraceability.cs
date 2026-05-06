using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track3DocumentReplacementTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_DocumentAreaCode_EntityType_EntityId",
                table: "StoredDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedDocumentId",
                table: "StoredDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplacementGroupKey",
                table: "StoredDocuments",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByDocumentId",
                table: "StoredDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [StoredDocuments]
                SET [ReplacementGroupKey] = CONCAT(
                    UPPER([DocumentAreaCode]),
                    ':',
                    UPPER([EntityType]),
                    ':',
                    LOWER(REPLACE(CONVERT(nvarchar(36), [EntityId]), '-', '')))
                WHERE [ReplacementGroupKey] = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_DocumentAreaCode_EntityType_EntityId_StatusCode",
                table: "StoredDocuments",
                columns: new[] { "DocumentAreaCode", "EntityType", "EntityId", "StatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_ReplacedDocumentId",
                table: "StoredDocuments",
                column: "ReplacedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_ReplacementGroupKey",
                table: "StoredDocuments",
                column: "ReplacementGroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_SupersededByDocumentId",
                table: "StoredDocuments",
                column: "SupersededByDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_DocumentAreaCode_EntityType_EntityId_StatusCode",
                table: "StoredDocuments");

            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_ReplacedDocumentId",
                table: "StoredDocuments");

            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_ReplacementGroupKey",
                table: "StoredDocuments");

            migrationBuilder.DropIndex(
                name: "IX_StoredDocuments_SupersededByDocumentId",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "ReplacedDocumentId",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "ReplacementGroupKey",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "SupersededByDocumentId",
                table: "StoredDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_DocumentAreaCode_EntityType_EntityId",
                table: "StoredDocuments",
                columns: new[] { "DocumentAreaCode", "EntityType", "EntityId" },
                unique: true);
        }
    }
}
