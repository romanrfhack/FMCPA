using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMCPA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Track5FinancialPermitRenewal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentRootPermitId",
                table: "FinancialPermits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentVersion",
                table: "FinancialPermits",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewalSequence",
                table: "FinancialPermits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RenewedFromPermitId",
                table: "FinancialPermits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [FinancialPermits]
                SET [CurrentRootPermitId] = [Id]
                WHERE [CurrentRootPermitId] IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentRootPermitId",
                table: "FinancialPermits",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPermits_CurrentRootPermitId",
                table: "FinancialPermits",
                column: "CurrentRootPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPermits_CurrentRootPermitId_RenewalSequence",
                table: "FinancialPermits",
                columns: new[] { "CurrentRootPermitId", "RenewalSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPermits_IsCurrentVersion",
                table: "FinancialPermits",
                column: "IsCurrentVersion");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPermits_RenewedFromPermitId",
                table: "FinancialPermits",
                column: "RenewedFromPermitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialPermits_CurrentRootPermitId",
                table: "FinancialPermits");

            migrationBuilder.DropIndex(
                name: "IX_FinancialPermits_CurrentRootPermitId_RenewalSequence",
                table: "FinancialPermits");

            migrationBuilder.DropIndex(
                name: "IX_FinancialPermits_IsCurrentVersion",
                table: "FinancialPermits");

            migrationBuilder.DropIndex(
                name: "IX_FinancialPermits_RenewedFromPermitId",
                table: "FinancialPermits");

            migrationBuilder.DropColumn(
                name: "CurrentRootPermitId",
                table: "FinancialPermits");

            migrationBuilder.DropColumn(
                name: "IsCurrentVersion",
                table: "FinancialPermits");

            migrationBuilder.DropColumn(
                name: "RenewalSequence",
                table: "FinancialPermits");

            migrationBuilder.DropColumn(
                name: "RenewedFromPermitId",
                table: "FinancialPermits");
        }
    }
}
