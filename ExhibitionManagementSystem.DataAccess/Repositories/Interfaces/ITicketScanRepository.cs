using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface ITicketScanRepository : IGenericRepository<TicketScan>
    {
        Task<IReadOnlyList<TicketScan>> GetByTicketAsync(int ticketId);
        Task<TicketScan?> GetLastScanAsync(int ticketId);
        Task<int> GetTodayScansCountAsync(int exhibitionId);
    }
}
