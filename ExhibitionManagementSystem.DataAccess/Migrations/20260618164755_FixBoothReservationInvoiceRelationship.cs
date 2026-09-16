using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixBoothReservationInvoiceRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoothReservations_Invoices_InvoiceID",
                table: "BoothReservations");

            migrationBuilder.DropIndex(
                name: "IX_BoothReservations_InvoiceID",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "InvoiceID",
                table: "BoothReservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceID",
                table: "BoothReservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BoothReservations_InvoiceID",
                table: "BoothReservations",
                column: "InvoiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_BoothReservations_Invoices_InvoiceID",
                table: "BoothReservations",
                column: "InvoiceID",
                principalTable: "Invoices",
                principalColumn: "InvoiceID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
