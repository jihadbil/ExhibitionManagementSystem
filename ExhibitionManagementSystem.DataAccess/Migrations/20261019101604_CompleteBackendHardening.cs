using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CompleteBackendHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop before DDL; never choose a sponsor or discard assignment history.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM SponsorAdAssignments WHERE IsDeleted = 0 GROUP BY SpaceID HAVING COUNT(*) > 1)
                    THROW 51000, 'Reconcile duplicate active advertising assignments before upgrading.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_SponsorAdAssignments_SpaceID",
                table: "SponsorAdAssignments");

            migrationBuilder.CreateSequence(
                name: "InvoiceNumbers");

            migrationBuilder.Sql("""
                DECLARE @last bigint = (
                    SELECT ISNULL(MAX(TRY_CONVERT(bigint, RIGHT(InvoiceNumber,
                        CHARINDEX('-', REVERSE(InvoiceNumber) + '-') - 1))), 0)
                    FROM Invoices WHERE InvoiceNumber LIKE 'INV-%');
                IF @last = 9223372036854775807
                    THROW 51000, 'Invoice sequence exhausted; review existing invoice numbers.', 1;
                DECLARE @next bigint = CASE WHEN @last < 0 THEN 1 ELSE @last + 1 END;
                DECLARE @restart nvarchar(150) = N'ALTER SEQUENCE [InvoiceNumbers] RESTART WITH ' + CONVERT(nvarchar(20), @next);
                EXEC sp_executesql @restart;
                """);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.CreateTable(
                name: "PasswordResetMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeasedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorAdAssignments_SpaceID",
                table: "SponsorAdAssignments",
                column: "SpaceID",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceID_IdempotencyKey",
                table: "Payments",
                columns: new[] { "InvoiceID", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetMessages_CompletedAt_NextAttemptAt",
                table: "PasswordResetMessages",
                columns: new[] { "CompletedAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetMessages_UserId_CreatedAt",
                table: "PasswordResetMessages",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Payments WHERE IdempotencyKey IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM PasswordResetMessages)
                    THROW 51000, 'Downgrade would discard payment replay or password reset history.', 1;
                """);
            migrationBuilder.DropTable(
                name: "PasswordResetMessages");

            migrationBuilder.DropIndex(
                name: "IX_SponsorAdAssignments_SpaceID",
                table: "SponsorAdAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_InvoiceID_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropSequence(
                name: "InvoiceNumbers");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorAdAssignments_SpaceID",
                table: "SponsorAdAssignments",
                column: "SpaceID");
        }
    }
}
