using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public sealed class InvoicePageDto : PagedResultDto<InvoiceDto>
{
    public int AllCount { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public int OverdueCount { get; set; }
}
