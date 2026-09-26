using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Tickets;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public class TicketScanPolicyTests
{
    [Fact]
    public void ActiveTicket_AllowsFirstEntryAndMarksTicketUsed()
    {
        var result = TicketScanPolicy.Evaluate(TicketStatus.Active, ScanDirection.In, null);

        Assert.True(result.IsAllowed);
        Assert.True(result.MarkTicketUsed);
    }

    [Theory]
    [InlineData(TicketStatus.Used, ScanDirection.In, ScanDirection.In, "TICKET_ALREADY_USED")]
    [InlineData(TicketStatus.Used, ScanDirection.In, ScanDirection.Out, "TICKET_ALREADY_USED")]
    [InlineData(TicketStatus.Active, ScanDirection.Out, null, "TICKET_NOT_INSIDE")]
    [InlineData(TicketStatus.Used, ScanDirection.Out, ScanDirection.Out, "TICKET_NOT_INSIDE")]
    [InlineData(TicketStatus.Cancelled, ScanDirection.In, null, "TICKET_NOT_VALID")]
    [InlineData(TicketStatus.Expired, ScanDirection.In, null, "TICKET_NOT_VALID")]
    public void InvalidSequence_IsRejected(
        TicketStatus status,
        ScanDirection requestedDirection,
        ScanDirection? lastDirection,
        string expectedError)
    {
        var result = TicketScanPolicy.Evaluate(status, requestedDirection, lastDirection);

        Assert.False(result.IsAllowed);
        Assert.Equal(expectedError, result.ErrorCode);
    }

    [Fact]
    public void UsedTicket_AllowsOneExitAfterEntry()
    {
        var result = TicketScanPolicy.Evaluate(
            TicketStatus.Used,
            ScanDirection.Out,
            ScanDirection.In);

        Assert.True(result.IsAllowed);
        Assert.False(result.MarkTicketUsed);
    }
}
