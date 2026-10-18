using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RepairFinancialWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoothMergeItems_BoothMerges_MergeID",
                table: "BoothMergeItems");

            migrationBuilder.DropIndex(
                name: "IX_BoothMergeItems_MergeID",
                table: "BoothMergeItems");

            migrationBuilder.AddColumn<int>(
                name: "ReservationServiceID",
                table: "InvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExhibitionID",
                table: "BoothMergeItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE Invoices SET TotalAmountInBaseCurrency = ROUND(TotalAmount * ExchangeRateUsed, 2)
                    WHERE ExchangeRateUsed > 0;
                UPDATE BoothReservations SET AmountInBaseCurrency = ROUND(TotalAmount * ExchangeRateUsed, 2)
                    WHERE ExchangeRateUsed > 0;
                UPDATE mi SET ExhibitionID = m.ExhibitionID
                FROM BoothMergeItems mi JOIN BoothMerges m ON m.MergeID = mi.MergeID;

                UPDATE b SET IsMerged = 0, MergeID = NULL,
                    Status = CASE WHEN Status = N'Merged' THEN N'Available' ELSE Status END
                FROM Booths b WHERE EXISTS (SELECT 1 FROM BoothMergeItems mi WHERE mi.BoothID = b.BoothID);

                WITH Matches AS (
                    SELECT item.InvoiceItemID, rs.ReservationServiceID,
                        COUNT(*) OVER (PARTITION BY item.InvoiceItemID) AS ItemMatches,
                        COUNT(*) OVER (PARTITION BY rs.ReservationServiceID) AS ServiceMatches
                    FROM InvoiceItems item
                    JOIN Invoices i ON i.InvoiceID = item.InvoiceID
                    JOIN ReservationServices rs ON rs.ReservationID = i.ReservationID
                    JOIN Services s ON s.ServiceID = rs.ServiceID
                    WHERE item.ItemName = s.ServiceName AND item.Quantity = rs.Quantity
                        AND item.UnitPrice = rs.UnitPrice AND item.TotalPrice = rs.TotalPrice
                )
                UPDATE item SET ReservationServiceID = m.ReservationServiceID
                FROM InvoiceItems item JOIN Matches m ON m.InvoiceItemID = item.InvoiceItemID
                WHERE m.ItemMatches = 1 AND m.ServiceMatches = 1;

                WITH Matches AS (
                    SELECT item.InvoiceItemID, rs.ReservationServiceID,
                        COUNT(*) OVER (PARTITION BY item.InvoiceItemID) AS ItemMatches,
                        COUNT(*) OVER (PARTITION BY rs.ReservationServiceID) AS ServiceMatches
                    FROM InvoiceItems item
                    JOIN Invoices i ON i.InvoiceID = item.InvoiceID
                    JOIN ReservationServices rs ON rs.ReservationID = i.ReservationID
                    WHERE item.ReservationServiceID IS NULL AND item.ItemName NOT LIKE N'حجز جناح%'
                        AND item.Quantity = rs.Quantity AND item.UnitPrice = rs.UnitPrice AND item.TotalPrice = rs.TotalPrice
                        AND NOT EXISTS (SELECT 1 FROM InvoiceItems linked WHERE linked.ReservationServiceID = rs.ReservationServiceID)
                )
                UPDATE item SET ReservationServiceID = m.ReservationServiceID
                FROM InvoiceItems item JOIN Matches m ON m.InvoiceItemID = item.InvoiceItemID
                WHERE m.ItemMatches = 1 AND m.ServiceMatches = 1;
            ");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_BoothMerges_MergeID_ExhibitionID",
                table: "BoothMerges",
                columns: new[] { "MergeID", "ExhibitionID" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServicePriceRules_Price_NonNegative",
                table: "ServicePriceRules",
                sql: "UnitPrice >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ReservationServiceID",
                table: "InvoiceItems",
                column: "ReservationServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_BoothMergeItems_ExhibitionID_BoothID",
                table: "BoothMergeItems",
                columns: new[] { "ExhibitionID", "BoothID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoothMergeItems_MergeID_ExhibitionID",
                table: "BoothMergeItems",
                columns: new[] { "MergeID", "ExhibitionID" });

            migrationBuilder.AddForeignKey(
                name: "FK_BoothMergeItems_BoothMerges_MergeID_ExhibitionID",
                table: "BoothMergeItems",
                columns: new[] { "MergeID", "ExhibitionID" },
                principalTable: "BoothMerges",
                principalColumns: new[] { "MergeID", "ExhibitionID" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_ReservationServices_ReservationServiceID",
                table: "InvoiceItems",
                column: "ReservationServiceID",
                principalTable: "ReservationServices",
                principalColumn: "ReservationServiceID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoothMergeItems_BoothMerges_MergeID_ExhibitionID",
                table: "BoothMergeItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_ReservationServices_ReservationServiceID",
                table: "InvoiceItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServicePriceRules_Price_NonNegative",
                table: "ServicePriceRules");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_ReservationServiceID",
                table: "InvoiceItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_BoothMerges_MergeID_ExhibitionID",
                table: "BoothMerges");

            migrationBuilder.DropIndex(
                name: "IX_BoothMergeItems_ExhibitionID_BoothID",
                table: "BoothMergeItems");

            migrationBuilder.DropIndex(
                name: "IX_BoothMergeItems_MergeID_ExhibitionID",
                table: "BoothMergeItems");

            migrationBuilder.DropColumn(
                name: "ReservationServiceID",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ExhibitionID",
                table: "BoothMergeItems");

            migrationBuilder.CreateIndex(
                name: "IX_BoothMergeItems_MergeID",
                table: "BoothMergeItems",
                column: "MergeID");

            migrationBuilder.AddForeignKey(
                name: "FK_BoothMergeItems_BoothMerges_MergeID",
                table: "BoothMergeItems",
                column: "MergeID",
                principalTable: "BoothMerges",
                principalColumn: "MergeID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
