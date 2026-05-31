using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class TicketScanRepository : GenericRepository<TicketScan>, ITicketScanRepository
    {
        public TicketScanRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<TicketScan>> GetByTicketAsync(int ticketId)
        {
            return await FindAsync(s => s.TicketID == ticketId);
        }

        public async Task<TicketScan?> GetLastScanAsync(int ticketId)
        {
            return await _dbSet.AsNoTracking()
                .Where(s => s.TicketID == ticketId)
                .OrderByDescending(s => s.ScanDateTime)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetTodayScansCountAsync(int exhibitionId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.AsNoTracking()
                .Include(s => s.Ticket)
                .Where(s => s.Ticket.ExhibitionID == exhibitionId && s.ScanDateTime.Date == today)
                .CountAsync();
        }
    }
}
