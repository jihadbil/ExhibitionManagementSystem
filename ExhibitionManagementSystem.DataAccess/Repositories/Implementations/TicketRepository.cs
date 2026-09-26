using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class TicketRepository : GenericRepository<Ticket>, ITicketRepository
    {
        public TicketRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Ticket?> GetByQRCodeAsync(int tenantId, string qrCode)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantID == tenantId && t.QRCode == qrCode);
        }

        public async Task<IReadOnlyList<Ticket>> GetByVisitorAsync(int visitorId)
        {
            return await FindAsync(t => t.VisitorID == visitorId);
        }

        public async Task<IReadOnlyList<Ticket>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(t => t.ExhibitionID == exhibitionId);
        }

        public async Task<IReadOnlyList<Ticket>> GetByStatusAsync(int exhibitionId, TicketStatus status)
        {
            return await FindAsync(t => t.ExhibitionID == exhibitionId && t.Status == status);
        }

        public async Task<bool> IsQRCodeUniqueAsync(string qrCode)
        {
            return !await _dbSet.IgnoreQueryFilters().AnyAsync(t => t.QRCode == qrCode);
        }

        public async Task<int> GetActiveTicketCountAsync(int exhibitionId)
        {
            return await CountAsync(t => t.ExhibitionID == exhibitionId && t.Status == TicketStatus.Active);
        }
    }
}
