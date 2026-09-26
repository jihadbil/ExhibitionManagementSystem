using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EnforceReservationAndPaymentConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BoothReservations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "TenantID",
                table: "BoothReservations",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE reservations
                SET reservations.TenantID = exhibitions.TenantID
                FROM BoothReservations AS reservations
                INNER JOIN Exhibitions AS exhibitions
                    ON exhibitions.ExhibitionID = reservations.ExhibitionID;

                IF EXISTS (SELECT 1 FROM BoothReservations WHERE TenantID IS NULL)
                    THROW 51000, 'Cannot backfill TenantID for every booth reservation.', 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "TenantID",
                table: "BoothReservations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM BoothReservations
                    WHERE BoothID IS NOT NULL
                      AND IsDeleted = 0
                      AND Status <> N'Cancelled'
                    GROUP BY ExhibitionID, BoothID
                    HAVING COUNT(*) > 1)
                    THROW 51001, 'Duplicate active booth reservations must be resolved before migration.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM BoothReservations
                    WHERE MergeID IS NOT NULL
                      AND IsDeleted = 0
                      AND Status <> N'Cancelled'
                    GROUP BY ExhibitionID, MergeID
                    HAVING COUNT(*) > 1)
                    THROW 51002, 'Duplicate active merged-booth reservations must be resolved before migration.', 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BoothReservations_TenantID",
                table: "BoothReservations",
                column: "TenantID");

            migrationBuilder.CreateIndex(
                name: "UX_BoothReservations_ActiveBooth",
                table: "BoothReservations",
                columns: new[] { "ExhibitionID", "BoothID" },
                unique: true,
                filter: "[BoothID] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> N'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "UX_BoothReservations_ActiveMerge",
                table: "BoothReservations",
                columns: new[] { "ExhibitionID", "MergeID" },
                unique: true,
                filter: "[MergeID] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> N'Cancelled'");

            migrationBuilder.AddForeignKey(
                name: "FK_BoothReservations_Tenants_TenantID",
                table: "BoothReservations",
                column: "TenantID",
                principalTable: "Tenants",
                principalColumn: "TenantID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoothReservations_Tenants_TenantID",
                table: "BoothReservations");

            migrationBuilder.DropIndex(
                name: "IX_BoothReservations_TenantID",
                table: "BoothReservations");

            migrationBuilder.DropIndex(
                name: "UX_BoothReservations_ActiveBooth",
                table: "BoothReservations");

            migrationBuilder.DropIndex(
                name: "UX_BoothReservations_ActiveMerge",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "TenantID",
                table: "BoothReservations");
        }
    }
}
