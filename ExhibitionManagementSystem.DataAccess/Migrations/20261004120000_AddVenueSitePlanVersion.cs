using ExhibitionManagementSystem.DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261004120000_AddVenueSitePlanVersion")]
    public partial class AddVenueSitePlanVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SitePlanVersion",
                table: "Venues",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SitePlanVersion",
                table: "Venues");
        }
    }
}
