using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCriticalFinancialCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Price_NonNegative",
                table: "Tickets",
                sql: "Price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SponsorshipPackages_MaxSponsors_Positive",
                table: "SponsorshipPackages",
                sql: "MaxSponsorsCount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SponsorshipPackages_Price_NonNegative",
                table: "SponsorshipPackages",
                sql: "Price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SponsorshipContracts_TotalAmount_NonNegative",
                table: "SponsorshipContracts",
                sql: "TotalAmount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments",
                sql: "Amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InvoiceItems_Quantity_Positive",
                table: "InvoiceItems",
                sql: "Quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InvoiceItems_TotalPrice_NonNegative",
                table: "InvoiceItems",
                sql: "TotalPrice >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InvoiceItems_UnitPrice_NonNegative",
                table: "InvoiceItems",
                sql: "UnitPrice >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Expenses_Amount_Positive",
                table: "Expenses",
                sql: "Amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CreditNotes_Amount_Positive",
                table: "CreditNotes",
                sql: "Amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AdvertisingSpaces_BasePrice_NonNegative",
                table: "AdvertisingSpaces",
                sql: "BasePrice >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Price_NonNegative",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SponsorshipPackages_MaxSponsors_Positive",
                table: "SponsorshipPackages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SponsorshipPackages_Price_NonNegative",
                table: "SponsorshipPackages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SponsorshipContracts_TotalAmount_NonNegative",
                table: "SponsorshipContracts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InvoiceItems_Quantity_Positive",
                table: "InvoiceItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InvoiceItems_TotalPrice_NonNegative",
                table: "InvoiceItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InvoiceItems_UnitPrice_NonNegative",
                table: "InvoiceItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Expenses_Amount_Positive",
                table: "Expenses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CreditNotes_Amount_Positive",
                table: "CreditNotes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AdvertisingSpaces_BasePrice_NonNegative",
                table: "AdvertisingSpaces");
        }
    }
}
