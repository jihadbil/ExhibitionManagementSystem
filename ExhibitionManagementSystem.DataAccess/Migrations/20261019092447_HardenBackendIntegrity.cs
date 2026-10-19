using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExhibitionManagementSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HardenBackendIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Never choose between conflicting relationships or delete bookings
            // automatically. Stop before any DDL so operators can reconcile data.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Payments WHERE InvoiceID1 IS NOT NULL AND InvoiceID1 <> InvoiceID)
                    THROW 51000, 'Payments contain conflicting invoice relationships; reconcile before upgrading.', 1;
                IF EXISTS (SELECT 1 FROM Tickets WHERE VisitorID1 IS NOT NULL AND VisitorID1 <> VisitorID)
                    THROW 51000, 'Tickets contain conflicting visitor relationships; reconcile before upgrading.', 1;
                IF EXISTS (SELECT 1 FROM ExhibitionSchedules WHERE ExhibitionID1 IS NOT NULL AND ExhibitionID1 <> ExhibitionID)
                    THROW 51000, 'Schedules contain conflicting exhibition relationships; reconcile before upgrading.', 1;
                IF EXISTS (SELECT 1 FROM BoothReservations WHERE
                    (ExhibitionID1 IS NOT NULL AND ExhibitionID1 <> ExhibitionID) OR
                    (ExhibitorID1 IS NOT NULL AND ExhibitorID1 <> ExhibitorID))
                    THROW 51000, 'Reservations contain conflicting relationships; reconcile before upgrading.', 1;
                IF EXISTS (SELECT 1 FROM BoothReservations WHERE BoothID IS NOT NULL AND IsDeleted = 0 AND Status <> N'Cancelled'
                    GROUP BY ExhibitionID, BoothID HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate active booth bookings exist; reconcile before upgrading.', 1;
                IF EXISTS (SELECT 1 FROM BoothReservations WHERE MergeID IS NOT NULL AND IsDeleted = 0 AND Status <> N'Cancelled'
                    GROUP BY ExhibitionID, MergeID HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate active merged bookings exist; reconcile before upgrading.', 1;
                """);
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

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Booths",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BoothReservations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "AdvertisingSpaces",
                columns: table => new
                {
                    SpaceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    ExhibitionID = table.Column<int>(type: "int", nullable: false),
                    VenueID = table.Column<int>(type: "int", nullable: true),
                    HallID = table.Column<int>(type: "int", nullable: true),
                    SpaceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SpaceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SpaceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DimensionsDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    LocationNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertisingSpaces", x => x.SpaceID);
                    table.ForeignKey(
                        name: "FK_AdvertisingSpaces_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvertisingSpaces_Exhibitions_ExhibitionID",
                        column: x => x.ExhibitionID,
                        principalTable: "Exhibitions",
                        principalColumn: "ExhibitionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvertisingSpaces_Halls_HallID",
                        column: x => x.HallID,
                        principalTable: "Halls",
                        principalColumn: "HallID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvertisingSpaces_Tenants_TenantID",
                        column: x => x.TenantID,
                        principalTable: "Tenants",
                        principalColumn: "TenantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvertisingSpaces_Venues_VenueID",
                        column: x => x.VenueID,
                        principalTable: "Venues",
                        principalColumn: "VenueID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BadgeTemplates",
                columns: table => new
                {
                    TemplateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    ExhibitionID = table.Column<int>(type: "int", nullable: true),
                    TemplateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetParticipantType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WidthMm = table.Column<int>(type: "int", nullable: false),
                    HeightMm = table.Column<int>(type: "int", nullable: false),
                    Orientation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HeaderBgColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HeaderTextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccentColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CategoryBadgeText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowLogo = table.Column<bool>(type: "bit", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShowQRCode = table.Column<bool>(type: "bit", nullable: false),
                    ShowBarcode = table.Column<bool>(type: "bit", nullable: false),
                    ShowCompanyName = table.Column<bool>(type: "bit", nullable: false),
                    ShowJobTitle = table.Column<bool>(type: "bit", nullable: false),
                    ShowHallName = table.Column<bool>(type: "bit", nullable: false),
                    ShowDates = table.Column<bool>(type: "bit", nullable: false),
                    CustomFooterText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomZplTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeTemplates", x => x.TemplateID);
                    table.ForeignKey(
                        name: "FK_BadgeTemplates_Exhibitions_ExhibitionID",
                        column: x => x.ExhibitionID,
                        principalTable: "Exhibitions",
                        principalColumn: "ExhibitionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BadgeTemplates_Tenants_TenantID",
                        column: x => x.TenantID,
                        principalTable: "Tenants",
                        principalColumn: "TenantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sponsors",
                columns: table => new
                {
                    SponsorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyProfile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sponsors", x => x.SponsorID);
                    table.ForeignKey(
                        name: "FK_Sponsors_Tenants_TenantID",
                        column: x => x.TenantID,
                        principalTable: "Tenants",
                        principalColumn: "TenantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipPackages",
                columns: table => new
                {
                    PackageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    ExhibitionID = table.Column<int>(type: "int", nullable: false),
                    PackageName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MaxSponsorsCount = table.Column<int>(type: "int", nullable: false),
                    EntitlementsDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipPackages", x => x.PackageID);
                    table.ForeignKey(
                        name: "FK_SponsorshipPackages_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorshipPackages_Exhibitions_ExhibitionID",
                        column: x => x.ExhibitionID,
                        principalTable: "Exhibitions",
                        principalColumn: "ExhibitionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorshipPackages_Tenants_TenantID",
                        column: x => x.TenantID,
                        principalTable: "Tenants",
                        principalColumn: "TenantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipContracts",
                columns: table => new
                {
                    ContractID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantID = table.Column<int>(type: "int", nullable: false),
                    ExhibitionID = table.Column<int>(type: "int", nullable: false),
                    SponsorID = table.Column<int>(type: "int", nullable: false),
                    PackageID = table.Column<int>(type: "int", nullable: true),
                    ContractNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SpecialTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipContracts", x => x.ContractID);
                    table.ForeignKey(
                        name: "FK_SponsorshipContracts_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorshipContracts_Exhibitions_ExhibitionID",
                        column: x => x.ExhibitionID,
                        principalTable: "Exhibitions",
                        principalColumn: "ExhibitionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorshipContracts_Sponsors_SponsorID",
                        column: x => x.SponsorID,
                        principalTable: "Sponsors",
                        principalColumn: "SponsorID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorshipContracts_SponsorshipPackages_PackageID",
                        column: x => x.PackageID,
                        principalTable: "SponsorshipPackages",
                        principalColumn: "PackageID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorshipContracts_Tenants_TenantID",
                        column: x => x.TenantID,
                        principalTable: "Tenants",
                        principalColumn: "TenantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsorAdAssignments",
                columns: table => new
                {
                    AssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractID = table.Column<int>(type: "int", nullable: false),
                    SpaceID = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssetStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ArtworkFileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorAdAssignments", x => x.AssignmentID);
                    table.ForeignKey(
                        name: "FK_SponsorAdAssignments_AdvertisingSpaces_SpaceID",
                        column: x => x.SpaceID,
                        principalTable: "AdvertisingSpaces",
                        principalColumn: "SpaceID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorAdAssignments_SponsorshipContracts_ContractID",
                        column: x => x.ContractID,
                        principalTable: "SponsorshipContracts",
                        principalColumn: "ContractID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoothReservations_ExhibitionID_BoothID",
                table: "BoothReservations",
                columns: new[] { "ExhibitionID", "BoothID" },
                unique: true,
                filter: "[BoothID] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> N'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_BoothReservations_ExhibitionID_MergeID",
                table: "BoothReservations",
                columns: new[] { "ExhibitionID", "MergeID" },
                unique: true,
                filter: "[MergeID] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> N'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingSpaces_CurrencyCode",
                table: "AdvertisingSpaces",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingSpaces_ExhibitionID_SpaceCode",
                table: "AdvertisingSpaces",
                columns: new[] { "ExhibitionID", "SpaceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingSpaces_HallID",
                table: "AdvertisingSpaces",
                column: "HallID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingSpaces_TenantID",
                table: "AdvertisingSpaces",
                column: "TenantID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingSpaces_VenueID",
                table: "AdvertisingSpaces",
                column: "VenueID");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeTemplates_ExhibitionID",
                table: "BadgeTemplates",
                column: "ExhibitionID");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeTemplates_TenantID_ExhibitionID_TargetParticipantType",
                table: "BadgeTemplates",
                columns: new[] { "TenantID", "ExhibitionID", "TargetParticipantType" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorAdAssignments_ContractID_SpaceID",
                table: "SponsorAdAssignments",
                columns: new[] { "ContractID", "SpaceID" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorAdAssignments_SpaceID",
                table: "SponsorAdAssignments",
                column: "SpaceID");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_TenantID_IsDeleted",
                table: "Sponsors",
                columns: new[] { "TenantID", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_CurrencyCode",
                table: "SponsorshipContracts",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_ExhibitionID",
                table: "SponsorshipContracts",
                column: "ExhibitionID");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_PackageID",
                table: "SponsorshipContracts",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_SponsorID",
                table: "SponsorshipContracts",
                column: "SponsorID");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_TenantID_ContractNumber",
                table: "SponsorshipContracts",
                columns: new[] { "TenantID", "ContractNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPackages_CurrencyCode",
                table: "SponsorshipPackages",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPackages_ExhibitionID_IsDeleted",
                table: "SponsorshipPackages",
                columns: new[] { "ExhibitionID", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPackages_TenantID",
                table: "SponsorshipPackages",
                column: "TenantID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM BadgeTemplates) OR EXISTS (SELECT 1 FROM Sponsors)
                    OR EXISTS (SELECT 1 FROM SponsorshipPackages) OR EXISTS (SELECT 1 FROM SponsorshipContracts)
                    OR EXISTS (SELECT 1 FROM AdvertisingSpaces) OR EXISTS (SELECT 1 FROM SponsorAdAssignments)
                    THROW 51000, 'Downgrade would discard badge or sponsorship data; export and reconcile first.', 1;
                """);
            migrationBuilder.DropTable(
                name: "BadgeTemplates");

            migrationBuilder.DropTable(
                name: "SponsorAdAssignments");

            migrationBuilder.DropTable(
                name: "AdvertisingSpaces");

            migrationBuilder.DropTable(
                name: "SponsorshipContracts");

            migrationBuilder.DropTable(
                name: "Sponsors");

            migrationBuilder.DropTable(
                name: "SponsorshipPackages");

            migrationBuilder.DropIndex(
                name: "IX_BoothReservations_ExhibitionID_BoothID",
                table: "BoothReservations");

            migrationBuilder.DropIndex(
                name: "IX_BoothReservations_ExhibitionID_MergeID",
                table: "BoothReservations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Booths");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BoothReservations");

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
