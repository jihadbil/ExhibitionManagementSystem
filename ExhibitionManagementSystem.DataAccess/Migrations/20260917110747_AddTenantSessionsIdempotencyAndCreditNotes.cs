using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSessionsIdempotencyAndCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantID",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantID",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantID",
                table: "Booths",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                columns: table => new
                {
                    CreditNoteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.CreditNoteID);
                    table.ForeignKey(
                        name: "FK_CreditNotes_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Invoices_InvoiceID",
                        column: x => x.InvoiceID,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Tenants_TenantID",
                        column: x => x.TenantID,
                        principalTable: "Tenants",
                        principalColumn: "TenantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    IdempotencyRecordID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.IdempotencyRecordID);
                });

            migrationBuilder.CreateTable(
                name: "RefreshSessions",
                columns: table => new
                {
                    RefreshSessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedBySessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeviceDescription = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshSessions", x => x.RefreshSessionID);
                    table.ForeignKey(
                        name: "FK_RefreshSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE ticket
                SET TenantID = exhibition.TenantID
                FROM Tickets AS ticket
                INNER JOIN Exhibitions AS exhibition
                    ON exhibition.ExhibitionID = ticket.ExhibitionID;

                UPDATE payment
                SET TenantID = invoice.TenantID
                FROM Payments AS payment
                INNER JOIN Invoices AS invoice
                    ON invoice.InvoiceID = payment.InvoiceID;

                UPDATE booth
                SET TenantID = venue.TenantID
                FROM Booths AS booth
                INNER JOIN Halls AS hall ON hall.HallID = booth.HallID
                INNER JOIN Venues AS venue ON venue.VenueID = hall.VenueID;

                IF EXISTS (SELECT 1 FROM Tickets WHERE TenantID IS NULL)
                    THROW 51000, 'Cannot determine TenantID for one or more tickets.', 1;
                IF EXISTS (SELECT 1 FROM Payments WHERE TenantID IS NULL)
                    THROW 51001, 'Cannot determine TenantID for one or more payments.', 1;
                IF EXISTS (SELECT 1 FROM Booths WHERE TenantID IS NULL)
                    THROW 51002, 'Cannot determine TenantID for one or more booths.', 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "TenantID",
                table: "Tickets",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TenantID",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TenantID",
                table: "Booths",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO RefreshSessions
                    (RefreshSessionID, UserId, TenantID, TokenHash, CreatedAt, ExpiresAt,
                     LastUsedAt, RevokedAt, ReplacedBySessionID, CreatedByIp,
                     DeviceDescription, RevocationReason)
                SELECT NEWID(), Id, TenantID, RefreshToken, SYSUTCDATETIME(),
                       COALESCE(RefreshTokenExpiry, DATEADD(day, -1, SYSUTCDATETIME())),
                       NULL, NULL, NULL, NULL, NULL, 'Migrated from single-token storage'
                FROM AspNetUsers
                WHERE RefreshToken IS NOT NULL AND LTRIM(RTRIM(RefreshToken)) <> '';
                """);

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiry",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantID_ExhibitionID",
                table: "Tickets",
                columns: new[] { "TenantID", "ExhibitionID" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantID_InvoiceID",
                table: "Payments",
                columns: new[] { "TenantID", "InvoiceID" });

            migrationBuilder.CreateIndex(
                name: "IX_Booths_TenantID_HallID",
                table: "Booths",
                columns: new[] { "TenantID", "HallID" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CreatedByUserId",
                table: "CreditNotes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_InvoiceID",
                table: "CreditNotes",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantID_CreditNoteNumber",
                table: "CreditNotes",
                columns: new[] { "TenantID", "CreditNoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantID_InvoiceID",
                table: "CreditNotes",
                columns: new[] { "TenantID", "InvoiceID" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_ExpiresAt",
                table: "IdempotencyRecords",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_TenantID_Operation_Key",
                table: "IdempotencyRecords",
                columns: new[] { "TenantID", "Operation", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_TokenHash",
                table: "RefreshSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_UserId_RevokedAt_ExpiresAt",
                table: "RefreshSessions",
                columns: new[] { "UserId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Booths_Tenants_TenantID",
                table: "Booths",
                column: "TenantID",
                principalTable: "Tenants",
                principalColumn: "TenantID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Tenants_TenantID",
                table: "Payments",
                column: "TenantID",
                principalTable: "Tenants",
                principalColumn: "TenantID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Tenants_TenantID",
                table: "Tickets",
                column: "TenantID",
                principalTable: "Tenants",
                principalColumn: "TenantID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Booths_Tenants_TenantID",
                table: "Booths");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Tenants_TenantID",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Tenants_TenantID",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE appUser
                SET RefreshToken = latest.TokenHash,
                    RefreshTokenExpiry = latest.ExpiresAt
                FROM AspNetUsers AS appUser
                OUTER APPLY
                (
                    SELECT TOP (1) session.TokenHash, session.ExpiresAt
                    FROM RefreshSessions AS session
                    WHERE session.UserId = appUser.Id
                      AND session.RevokedAt IS NULL
                    ORDER BY session.CreatedAt DESC
                ) AS latest;
                """);

            migrationBuilder.DropTable(
                name: "CreditNotes");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "RefreshSessions");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TenantID_ExhibitionID",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantID_InvoiceID",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Booths_TenantID_HallID",
                table: "Booths");

            migrationBuilder.DropColumn(
                name: "TenantID",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TenantID",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TenantID",
                table: "Booths");

        }
    }
}
