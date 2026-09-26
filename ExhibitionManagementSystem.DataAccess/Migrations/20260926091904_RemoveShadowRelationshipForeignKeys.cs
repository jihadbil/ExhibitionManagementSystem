using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowRelationshipForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoothReservations_Exhibitions_ExhibitionID1",
                table: "BoothReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_BoothReservations_Exhibitors_ExhibitorID1",
                table: "BoothReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ExhibitionSchedules_Exhibitions_ExhibitionID1",
                table: "ExhibitionSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Invoices_InvoiceID1",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Visitors_VisitorID1",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_VisitorID1",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Payments_InvoiceID1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ExhibitionSchedules_ExhibitionID1",
                table: "ExhibitionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BoothReservations_ExhibitionID1",
                table: "BoothReservations");

            migrationBuilder.DropIndex(
                name: "IX_BoothReservations_ExhibitorID1",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "VisitorID1",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "InvoiceID1",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExhibitionID1",
                table: "ExhibitionSchedules");

            migrationBuilder.DropColumn(
                name: "ExhibitionID1",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "ExhibitorID1",
                table: "BoothReservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VisitorID1",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceID1",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExhibitionID1",
                table: "ExhibitionSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExhibitionID1",
                table: "BoothReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExhibitorID1",
                table: "BoothReservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_VisitorID1",
                table: "Tickets",
                column: "VisitorID1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceID1",
                table: "Payments",
                column: "InvoiceID1");

            migrationBuilder.CreateIndex(
                name: "IX_ExhibitionSchedules_ExhibitionID1",
                table: "ExhibitionSchedules",
                column: "ExhibitionID1");

            migrationBuilder.CreateIndex(
                name: "IX_BoothReservations_ExhibitionID1",
                table: "BoothReservations",
                column: "ExhibitionID1");

            migrationBuilder.CreateIndex(
                name: "IX_BoothReservations_ExhibitorID1",
                table: "BoothReservations",
                column: "ExhibitorID1");

            migrationBuilder.AddForeignKey(
                name: "FK_BoothReservations_Exhibitions_ExhibitionID1",
                table: "BoothReservations",
                column: "ExhibitionID1",
                principalTable: "Exhibitions",
                principalColumn: "ExhibitionID");

            migrationBuilder.AddForeignKey(
                name: "FK_BoothReservations_Exhibitors_ExhibitorID1",
                table: "BoothReservations",
                column: "ExhibitorID1",
                principalTable: "Exhibitors",
                principalColumn: "ExhibitorID");

            migrationBuilder.AddForeignKey(
                name: "FK_ExhibitionSchedules_Exhibitions_ExhibitionID1",
                table: "ExhibitionSchedules",
                column: "ExhibitionID1",
                principalTable: "Exhibitions",
                principalColumn: "ExhibitionID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Invoices_InvoiceID1",
                table: "Payments",
                column: "InvoiceID1",
                principalTable: "Invoices",
                principalColumn: "InvoiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Visitors_VisitorID1",
                table: "Tickets",
                column: "VisitorID1",
                principalTable: "Visitors",
                principalColumn: "VisitorID");
        }
    }
}
