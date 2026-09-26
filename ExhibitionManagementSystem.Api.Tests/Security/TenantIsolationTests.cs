using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task QueryFilter_ReturnsOnlyCurrentTenantAndActiveRows()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = CreateOptions(databaseName);

        await using (var seedContext = new ApplicationDbContext(
                         options,
                         new StubTenantContext(null, CanBypassTenantIsolation: true)))
        {
            seedContext.Venues.AddRange(
                CreateVenue(1, "Tenant 1 active"),
                CreateVenue(1, "Tenant 1 deleted", isDeleted: true),
                CreateVenue(2, "Tenant 2 active"));
            await seedContext.SaveChangesAsync();
        }

        await using var tenantContext = new ApplicationDbContext(
            options,
            new StubTenantContext(1));

        var venues = await tenantContext.Venues.ToListAsync();

        var venue = Assert.Single(venues);
        Assert.Equal(1, venue.TenantID);
        Assert.Equal("Tenant 1 active", venue.Name);
    }

    [Fact]
    public async Task SaveChanges_BlocksCrossTenantWrite()
    {
        var context = new ApplicationDbContext(
            CreateOptions(Guid.NewGuid().ToString("N")),
            new StubTenantContext(1));
        context.Venues.Add(CreateVenue(2, "Wrong tenant"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Equal("Cross-tenant write was blocked.", exception.Message);
    }

    [Fact]
    public async Task SaveChanges_AssignsCurrentTenantToNewEntity()
    {
        await using var context = new ApplicationDbContext(
            CreateOptions(Guid.NewGuid().ToString("N")),
            new StubTenantContext(7));
        var venue = CreateVenue(0, "New venue");
        context.Venues.Add(venue);

        await context.SaveChangesAsync();

        Assert.Equal(7, venue.TenantID);
    }

    [Fact]
    public async Task ReservationQueryFilter_DoesNotExposeAnotherTenant()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = CreateOptions(databaseName);

        await using (var seedContext = new ApplicationDbContext(
                         options,
                         new StubTenantContext(null, CanBypassTenantIsolation: true)))
        {
            seedContext.BoothReservations.AddRange(
                CreateReservation(1, 101),
                CreateReservation(2, 202));
            await seedContext.SaveChangesAsync();
        }

        await using var tenantContext = new ApplicationDbContext(
            options,
            new StubTenantContext(1));

        var reservations = await tenantContext.BoothReservations.ToListAsync();

        var reservation = Assert.Single(reservations);
        Assert.Equal(1, reservation.TenantID);
        Assert.Equal(101, reservation.ReservationID);
    }

    [Fact]
    public void ReservationAndInvoiceModels_ExposeConcurrencyAndUniquenessGuards()
    {
        using var context = new ApplicationDbContext(
            CreateOptions(Guid.NewGuid().ToString("N")),
            new StubTenantContext(1));
        var model = context.Model;

        var reservationType = model.FindEntityType(typeof(BoothReservation));
        var invoiceType = model.FindEntityType(typeof(Invoice));

        Assert.True(reservationType!.FindProperty(nameof(BoothReservation.RowVersion))!.IsConcurrencyToken);
        Assert.True(invoiceType!.FindProperty(nameof(Invoice.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(reservationType.GetIndexes(), index =>
            index.IsUnique && index.GetDatabaseName() == "UX_BoothReservations_ActiveBooth");
        Assert.Contains(reservationType.GetIndexes(), index =>
            index.IsUnique && index.GetDatabaseName() == "UX_BoothReservations_ActiveMerge");
    }

    [Fact]
    public void IndirectEntities_AreTenantOwnedAndFiltered()
    {
        using var context = new ApplicationDbContext(
            CreateOptions(Guid.NewGuid().ToString("N")),
            new StubTenantContext(1));

        Assert.IsAssignableFrom<ITenantEntity>(new Booth());
        Assert.IsAssignableFrom<ITenantEntity>(new Ticket());
        Assert.IsAssignableFrom<ITenantEntity>(new Payment());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(Booth))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(Ticket))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(Payment))!.GetDeclaredQueryFilters());
    }

    [Fact]
    public async Task IssuedInvoice_FinancialFieldsCannotBeChanged()
    {
        await using var context = new ApplicationDbContext(
            CreateOptions(Guid.NewGuid().ToString("N")),
            new StubTenantContext(1));
        var invoice = new Invoice
        {
            TenantID = 1,
            ReservationID = 10,
            InvoiceNumber = "INV-1",
            CurrencyCode = "LYD",
            TotalAmount = 100,
            Status = InvoiceStatus.Issued
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        invoice.TotalAmount = 50;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
        Assert.Contains("immutable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RefreshSessions_HaveUniqueTokenHashAndConcurrencyGuard()
    {
        using var context = new ApplicationDbContext(
            CreateOptions(Guid.NewGuid().ToString("N")),
            new StubTenantContext(1));
        var sessionType = context.Model.FindEntityType(typeof(RefreshSession))!;

        Assert.True(sessionType.FindProperty(nameof(RefreshSession.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(sessionType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(RefreshSession.TokenHash));
    }

    [Fact]
    public void TenantInvitations_HaveOneTimeTokenAndPendingEmailGuards()
    {
        using var context = new ApplicationDbContext(
            CreateOptions(Guid.NewGuid().ToString("N")),
            new StubTenantContext(null, CanBypassTenantIsolation: true));
        var invitationType = context.Model.FindEntityType(typeof(TenantInvitation))!;

        Assert.True(invitationType.FindProperty(nameof(TenantInvitation.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(invitationType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(TenantInvitation.TokenHash));
        Assert.Contains(invitationType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(TenantInvitation.NormalizedEmail) &&
            index.GetFilter() == "[AcceptedAt] IS NULL AND [RevokedAt] IS NULL");
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(string databaseName) =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

    private static Venue CreateVenue(int tenantId, string name, bool isDeleted = false) =>
        new()
        {
            TenantID = tenantId,
            Name = name,
            Address = "Test address",
            City = "Test city",
            Country = "Test country",
            MapImageURL = string.Empty,
            IsDeleted = isDeleted
        };

    private static BoothReservation CreateReservation(int tenantId, int id) => new()
    {
        ReservationID = id,
        TenantID = tenantId,
        ExhibitorID = id,
        ExhibitionID = id,
        BoothTypeSelected = BoothType.Standard,
        ExhibitorCategory = ExhibitorCategory.Local,
        CurrencyCode = "LYD",
        LogisticNotes = string.Empty,
        CreatedByUserId = "test-user",
        Status = ReservationStatus.Pending
    };

    private sealed record StubTenantContext(
        int? TenantId,
        bool CanBypassTenantIsolation = false) : ITenantContext;
}
