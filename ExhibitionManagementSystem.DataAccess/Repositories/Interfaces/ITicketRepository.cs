using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface ITicketRepository : IGenericRepository<Ticket>
    {
        Task<Ticket?> GetByQRCodeAsync(string qrCode);
        Task<IReadOnlyList<Ticket>> GetByVisitorAsync(int visitorId);
        Task<IReadOnlyList<Ticket>> GetByExhibitionAsync(int exhibitionId);
        Task<IReadOnlyList<Ticket>> GetByStatusAsync(int exhibitionId, TicketStatus status);
        Task<bool> IsQRCodeUniqueAsync(string qrCode);
        Task<int> GetActiveTicketCountAsync(int exhibitionId);
    }
}
