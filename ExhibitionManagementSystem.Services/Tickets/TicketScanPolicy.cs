using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Services.Tickets;

/// <summary>
/// Enforces the single-entry ticket scan state machine.
/// Re-entry can be introduced later as an explicit ticket-type policy.
/// </summary>
public static class TicketScanPolicy
{
    public static TicketScanPolicyResult Evaluate(
        TicketStatus status,
        ScanDirection requestedDirection,
        ScanDirection? lastDirection)
    {
        if (status is TicketStatus.Cancelled or TicketStatus.Expired)
        {
            return TicketScanPolicyResult.Reject(
                "TICKET_NOT_VALID",
                "التذكرة ملغاة أو منتهية الصلاحية.");
        }

        if (requestedDirection == ScanDirection.In)
        {
            if (status != TicketStatus.Active || lastDirection == ScanDirection.In)
            {
                return TicketScanPolicyResult.Reject(
                    "TICKET_ALREADY_USED",
                    "استُخدمت التذكرة للدخول مسبقًا ولا تسمح بإعادة الدخول.");
            }

            return TicketScanPolicyResult.Allow(markTicketUsed: true);
        }

        if (status != TicketStatus.Used || lastDirection != ScanDirection.In)
        {
            return TicketScanPolicyResult.Reject(
                "TICKET_NOT_INSIDE",
                "لا يمكن تسجيل الخروج قبل تسجيل دخول صالح.");
        }

        return TicketScanPolicyResult.Allow(markTicketUsed: false);
    }
}

public sealed record TicketScanPolicyResult(
    bool IsAllowed,
    bool MarkTicketUsed,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static TicketScanPolicyResult Allow(bool markTicketUsed) =>
        new(true, markTicketUsed, null, null);

    public static TicketScanPolicyResult Reject(string errorCode, string errorMessage) =>
        new(false, false, errorCode, errorMessage);
}
