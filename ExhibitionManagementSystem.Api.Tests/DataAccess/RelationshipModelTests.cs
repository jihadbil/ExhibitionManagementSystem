using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.DataAccess;

public sealed class RelationshipModelTests
{
    [Theory]
    [InlineData(typeof(BoothReservation), "ExhibitionID1")]
    [InlineData(typeof(BoothReservation), "ExhibitorID1")]
    [InlineData(typeof(ExhibitionSchedule), "ExhibitionID1")]
    [InlineData(typeof(Payment), "InvoiceID1")]
    [InlineData(typeof(Ticket), "VisitorID1")]
    public void Model_DoesNotContainDuplicateShadowForeignKeys(
        Type entityType,
        string propertyName)
    {
        using var context = CreateContext();

        var property = context.Model.FindEntityType(entityType)?.FindProperty(propertyName);

        Assert.Null(property);
    }

    [Fact]
    public void SponsorSpaceAssignment_HasOneActiveAssignmentConstraint()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(SponsorAdAssignment));

        var index = Assert.Single(entityType!.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == "UX_SponsorAdAssignments_ActiveSpace");

        Assert.True(index.IsUnique);
        Assert.Equal("[IsDeleted] = 0", index.GetFilter());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options, new BypassTenantContext());
    }

    private sealed class BypassTenantContext : ITenantContext
    {
        public int? TenantId => null;
        public bool CanBypassTenantIsolation => true;
    }
}
