using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class TenantProvisioningWriteScopeTests
{
    [Fact]
    public async Task ProvisioningScope_AllowsOnlyTheTrackedNewTenant()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new ApplicationDbContext(options);
        var tenant = new Tenant
        {
            CompanyName = "شركة اختبار",
            Subdomain = "registration-test",
            BaseCurrency = "LYD"
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        using (db.BeginTenantProvisioningScope(tenant.TenantID))
        {
            db.AuditLogs.Add(new AuditLog
            {
                TenantID = tenant.TenantID,
                UserId = "registration-user",
                TableName = "CompanyRegistration",
                RecordID = tenant.TenantID.ToString(),
                Action = "Created"
            });
            await db.SaveChangesAsync();
        }

        Assert.Equal(
            tenant.TenantID,
            (await db.AuditLogs.IgnoreQueryFilters().SingleAsync()).TenantID);
        Assert.Throws<InvalidOperationException>(() => db.BeginTenantProvisioningScope(tenant.TenantID + 1));
    }
}
