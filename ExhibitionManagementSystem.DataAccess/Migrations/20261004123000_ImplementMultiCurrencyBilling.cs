using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ImplementMultiCurrencyBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrencyCode",
                table: "Services",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LYD");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeRateDate",
                table: "ReservationServices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeRateSource",
                table: "ReservationServices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateUsed",
                table: "ReservationServices",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "ServicePriceRuleID",
                table: "ReservationServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCurrencyCode",
                table: "ReservationServices",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LYD");

            migrationBuilder.AddColumn<decimal>(
                name: "SourceTotalPrice",
                table: "ReservationServices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SourceUnitPrice",
                table: "ReservationServices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountInBaseCurrency",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BaseCurrencyCode",
                table: "Payments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LYD");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeRateDate",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateUsed",
                table: "Payments",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "BaseCurrencyCode",
                table: "Invoices",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LYD");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeRateDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateUsed",
                table: "Invoices",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmountInBaseCurrency",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountInBaseCurrency",
                table: "Expenses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BaseCurrencyCode",
                table: "Expenses",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LYD");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeRateDate",
                table: "Expenses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateUsed",
                table: "Expenses",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCurrencyCode",
                table: "Exhibitors",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LYD");

            migrationBuilder.AddColumn<string>(
                name: "BaseCurrencyCode",
                table: "BoothReservations",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LYD");

            migrationBuilder.AddColumn<DateTime>(
                name: "BoothExchangeRateDate",
                table: "BoothReservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoothExchangeRateSource",
                table: "BoothReservations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BoothExchangeRateUsed",
                table: "BoothReservations",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "BoothPriceRuleID",
                table: "BoothReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoothSourceAmount",
                table: "BoothReservations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BoothSourceCurrencyCode",
                table: "BoothReservations",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LYD");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeRateDate",
                table: "BoothReservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeRateSource",
                table: "BoothReservations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Preserve the meaning of existing financial records. Existing transaction
            // currencies are treated as their historical source currencies; base values
            // reuse the rate snapshot already stored on the reservation.
            migrationBuilder.Sql(@"
                UPDATE s SET DefaultCurrencyCode = t.BaseCurrency
                FROM Services s INNER JOIN Tenants t ON t.TenantID = s.TenantID;

                UPDATE e SET PreferredCurrencyCode = t.BaseCurrency
                FROM Exhibitors e INNER JOIN Tenants t ON t.TenantID = e.TenantID;

                UPDATE r SET
                    BaseCurrencyCode = t.BaseCurrency,
                    BoothSourceCurrencyCode = r.CurrencyCode,
                    BoothSourceAmount = r.BoothAmount,
                    BoothExchangeRateUsed = 1,
                    BoothExchangeRateDate = CAST(r.CreatedAt AS date),
                    BoothExchangeRateSource = N'Historical same-currency migration',
                    ExchangeRateDate = CAST(r.CreatedAt AS date),
                    ExchangeRateSource = N'Historical reservation snapshot'
                FROM BoothReservations r INNER JOIN Tenants t ON t.TenantID = r.TenantID;

                UPDATE rs SET
                    SourceCurrencyCode = rs.CurrencyCode,
                    SourceUnitPrice = rs.UnitPrice,
                    SourceTotalPrice = rs.TotalPrice,
                    ExchangeRateUsed = 1,
                    ExchangeRateDate = CAST(r.CreatedAt AS date),
                    ExchangeRateSource = N'Historical same-currency migration'
                FROM ReservationServices rs
                INNER JOIN BoothReservations r ON r.ReservationID = rs.ReservationID;

                UPDATE i SET
                    BaseCurrencyCode = r.BaseCurrencyCode,
                    ExchangeRateUsed = r.ExchangeRateUsed,
                    ExchangeRateDate = r.ExchangeRateDate,
                    TotalAmountInBaseCurrency = ROUND(i.TotalAmount * r.ExchangeRateUsed, 2)
                FROM Invoices i INNER JOIN BoothReservations r ON r.ReservationID = i.ReservationID;

                UPDATE p SET
                    BaseCurrencyCode = i.BaseCurrencyCode,
                    ExchangeRateUsed = i.ExchangeRateUsed,
                    ExchangeRateDate = i.ExchangeRateDate,
                    AmountInBaseCurrency = ROUND(p.Amount * i.ExchangeRateUsed, 2)
                FROM Payments p INNER JOIN Invoices i ON i.InvoiceID = p.InvoiceID;

                UPDATE x SET
                    BaseCurrencyCode = t.BaseCurrency,
                    ExchangeRateUsed = CASE WHEN x.CurrencyCode = t.BaseCurrency THEN 1 ELSE 0 END,
                    ExchangeRateDate = x.ExpenseDate,
                    AmountInBaseCurrency = CASE WHEN x.CurrencyCode = t.BaseCurrency THEN x.Amount ELSE 0 END
                FROM Expenses x INNER JOIN Tenants t ON t.TenantID = x.TenantID;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Services_DefaultCurrencyCode",
                table: "Services",
                column: "DefaultCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationServices_SourceCurrencyCode",
                table: "ReservationServices",
                column: "SourceCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BaseCurrencyCode",
                table: "Payments",
                column: "BaseCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BaseCurrencyCode",
                table: "Invoices",
                column: "BaseCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BaseCurrencyCode",
                table: "Expenses",
                column: "BaseCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Exhibitors_PreferredCurrencyCode",
                table: "Exhibitors",
                column: "PreferredCurrencyCode");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExchangeRates_DifferentCurrencies",
                table: "ExchangeRates",
                sql: "FromCurrency <> ToCurrency");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExchangeRates_Rate_Positive",
                table: "ExchangeRates",
                sql: "Rate > 0");

            migrationBuilder.CreateIndex(
                name: "IX_BoothReservations_BaseCurrencyCode",
                table: "BoothReservations",
                column: "BaseCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_BoothReservations_BoothSourceCurrencyCode",
                table: "BoothReservations",
                column: "BoothSourceCurrencyCode");

            migrationBuilder.AddForeignKey(
                name: "FK_BoothReservations_Currencies_BaseCurrencyCode",
                table: "BoothReservations",
                column: "BaseCurrencyCode",
                principalTable: "Currencies",
                principalColumn: "CurrencyCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoothReservations_Currencies_BoothSourceCurrencyCode",
                table: "BoothReservations",
                column: "BoothSourceCurrencyCode",
                principalTable: "Currencies",
                principalColumn: "CurrencyCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exhibitors_Currencies_PreferredCurrencyCode",
                table: "Exhibitors",
                column: "PreferredCurrencyCode",
                principalTable: "Currencies",
                principalColumn: "CurrencyCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Currencies_BaseCurrencyCode",
                table: "Expenses",
                column: "BaseCurrencyCode",
                principalTable: "Currencies",
                principalColumn: "CurrencyCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Currencies_BaseCurrencyCode",
                table: "Invoices",
                column: "BaseCurrencyCode",
                principalTable: "Currencies",
                principalColumn: "CurrencyCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Currencies_BaseCurrencyCode",
                table: "Payments",
                column: "BaseCurrencyCode",
                principalTable: "Currencies",
                principalColumn: "CurrencyCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationServices_Currencies_SourceCurrencyCode",
                table: "ReservationServices",
                column: "SourceCurrencyCode",
                principalTable: "Currencies",
                principalColumn: "CurrencyCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Currencies_DefaultCurrencyCode",
                table: "Services",
                column: "DefaultCurrencyCode",
                principalTable: "Currencies",
                principalColumn: "CurrencyCode",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoothReservations_Currencies_BaseCurrencyCode",
                table: "BoothReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_BoothReservations_Currencies_BoothSourceCurrencyCode",
                table: "BoothReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Exhibitors_Currencies_PreferredCurrencyCode",
                table: "Exhibitors");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Currencies_BaseCurrencyCode",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Currencies_BaseCurrencyCode",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Currencies_BaseCurrencyCode",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationServices_Currencies_SourceCurrencyCode",
                table: "ReservationServices");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Currencies_DefaultCurrencyCode",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_DefaultCurrencyCode",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_ReservationServices_SourceCurrencyCode",
                table: "ReservationServices");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BaseCurrencyCode",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_BaseCurrencyCode",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_BaseCurrencyCode",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Exhibitors_PreferredCurrencyCode",
                table: "Exhibitors");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExchangeRates_DifferentCurrencies",
                table: "ExchangeRates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExchangeRates_Rate_Positive",
                table: "ExchangeRates");

            migrationBuilder.DropIndex(
                name: "IX_BoothReservations_BaseCurrencyCode",
                table: "BoothReservations");

            migrationBuilder.DropIndex(
                name: "IX_BoothReservations_BoothSourceCurrencyCode",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "DefaultCurrencyCode",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "ReservationServices");

            migrationBuilder.DropColumn(
                name: "ExchangeRateSource",
                table: "ReservationServices");

            migrationBuilder.DropColumn(
                name: "ExchangeRateUsed",
                table: "ReservationServices");

            migrationBuilder.DropColumn(
                name: "ServicePriceRuleID",
                table: "ReservationServices");

            migrationBuilder.DropColumn(
                name: "SourceCurrencyCode",
                table: "ReservationServices");

            migrationBuilder.DropColumn(
                name: "SourceTotalPrice",
                table: "ReservationServices");

            migrationBuilder.DropColumn(
                name: "SourceUnitPrice",
                table: "ReservationServices");

            migrationBuilder.DropColumn(
                name: "AmountInBaseCurrency",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BaseCurrencyCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExchangeRateUsed",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BaseCurrencyCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRateUsed",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TotalAmountInBaseCurrency",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AmountInBaseCurrency",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "BaseCurrencyCode",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ExchangeRateUsed",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PreferredCurrencyCode",
                table: "Exhibitors");

            migrationBuilder.DropColumn(
                name: "BaseCurrencyCode",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "BoothExchangeRateDate",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "BoothExchangeRateSource",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "BoothExchangeRateUsed",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "BoothPriceRuleID",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "BoothSourceAmount",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "BoothSourceCurrencyCode",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "ExchangeRateSource",
                table: "BoothReservations");
        }
    }
}
