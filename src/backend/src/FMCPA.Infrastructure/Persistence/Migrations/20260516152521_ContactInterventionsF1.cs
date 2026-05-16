using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContactInterventionsF1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactInterventions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OriginId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OriginDisplayName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    HelpType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactInterventions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactInterventions_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactInterventions_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInterventions_ContactId_OccurredUtc",
                table: "ContactInterventions",
                columns: new[] { "ContactId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInterventions_CreatedByUserId_CreatedUtc",
                table: "ContactInterventions",
                columns: new[] { "CreatedByUserId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInterventions_ModuleKey_OriginType_OriginId_OccurredUtc",
                table: "ContactInterventions",
                columns: new[] { "ModuleKey", "OriginType", "OriginId", "OccurredUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactInterventions");
        }
    }
}
