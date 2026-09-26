using ExhibitionManagementSystem.DataAccess.Repositories;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Financial;

public sealed class InvoiceNumberFormatterTests
{
    [Fact]
    public void Format_UsesTenantYearAndPaddedSequence()
    {
        var result = InvoiceNumberFormatter.Format(12, 2026, 34);

        Assert.Equal("INV-0012-2026-00034", result);
    }

    [Theory]
    [InlineData(0, 2026, 1)]
    [InlineData(1, 1999, 1)]
    [InlineData(1, 2026, 0)]
    public void Format_RejectsInvalidComponents(int tenantId, int year, int sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InvoiceNumberFormatter.Format(tenantId, year, sequence));
    }
}
