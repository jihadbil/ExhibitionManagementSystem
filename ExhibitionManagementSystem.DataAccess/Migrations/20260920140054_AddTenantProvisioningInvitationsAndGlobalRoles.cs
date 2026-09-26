using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantProvisioningInvitationsAndGlobalRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_Tenants_TenantID",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_TenantID",
                table: "AspNetRoles");

            migrationBuilder.AlterColumn<int>(
                name: "TenantID",
                table: "AspNetRoles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "TenantInvitations",
                columns: table => new
                {
                    InvitationID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvitations", x => x.InvitationID);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Tenants_TenantID",
                        column: x => x.TenantID,
                        principalTable: "Tenants",
                        principalColumn: "TenantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_NormalizedEmail",
                table: "TenantInvitations",
                column: "NormalizedEmail",
                unique: true,
                filter: "[AcceptedAt] IS NULL AND [RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantID_NormalizedEmail_AcceptedAt_RevokedAt",
                table: "TenantInvitations",
                columns: new[] { "TenantID", "NormalizedEmail", "AcceptedAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TokenHash",
                table: "TenantInvitations",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantInvitations");

            migrationBuilder.Sql(
                """
                UPDATE roles
                SET TenantID = COALESCE(
                    (
                        SELECT MIN(users.TenantID)
                        FROM AspNetUserRoles userRoles
                        INNER JOIN AspNetUsers users ON users.Id = userRoles.UserId
                        WHERE userRoles.RoleId = roles.Id
                    ),
                    (SELECT MIN(TenantID) FROM Tenants)
                )
                FROM AspNetRoles roles
                WHERE roles.TenantID IS NULL;

                IF EXISTS (SELECT 1 FROM AspNetRoles WHERE TenantID IS NULL)
                    THROW 51000, 'Cannot restore tenant-scoped roles because no tenant exists.', 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "TenantID",
                table: "AspNetRoles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_TenantID",
                table: "AspNetRoles",
                column: "TenantID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_Tenants_TenantID",
                table: "AspNetRoles",
                column: "TenantID",
                principalTable: "Tenants",
                principalColumn: "TenantID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
