using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveSponsorSpaceAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SponsorAdAssignments_SpaceID",
                table: "SponsorAdAssignments");

            migrationBuilder.CreateIndex(
                name: "UX_SponsorAdAssignments_ActiveSpace",
                table: "SponsorAdAssignments",
                column: "SpaceID",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SponsorAdAssignments_ActiveSpace",
                table: "SponsorAdAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorAdAssignments_SpaceID",
                table: "SponsorAdAssignments",
                column: "SpaceID");
        }
    }
}
