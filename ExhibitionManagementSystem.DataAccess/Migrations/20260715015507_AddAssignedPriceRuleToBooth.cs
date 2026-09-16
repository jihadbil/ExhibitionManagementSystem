using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedPriceRuleToBooth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedPriceRuleID",
                table: "Booths",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Booths_AssignedPriceRuleID",
                table: "Booths",
                column: "AssignedPriceRuleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Booths_BoothPriceRules_AssignedPriceRuleID",
                table: "Booths",
                column: "AssignedPriceRuleID",
                principalTable: "BoothPriceRules",
                principalColumn: "RuleID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Booths_BoothPriceRules_AssignedPriceRuleID",
                table: "Booths");

            migrationBuilder.DropIndex(
                name: "IX_Booths_AssignedPriceRuleID",
                table: "Booths");

            migrationBuilder.DropColumn(
                name: "AssignedPriceRuleID",
                table: "Booths");
        }
    }
}
