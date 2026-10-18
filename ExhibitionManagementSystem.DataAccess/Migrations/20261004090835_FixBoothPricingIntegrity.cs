using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixBoothPricingIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoothType",
                table: "Booths",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.CreateIndex(
                name: "UX_Booths_ActiveNumberPerHall",
                table: "Booths",
                columns: new[] { "HallID", "BoothNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Booths_ActiveNumberPerHall",
                table: "Booths");

            migrationBuilder.DropColumn(
                name: "BoothType",
                table: "Booths");
        }
    }
}
