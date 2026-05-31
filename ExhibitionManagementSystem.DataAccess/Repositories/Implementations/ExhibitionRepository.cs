using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class ExhibitionRepository : GenericRepository<Exhibition>, IExhibitionRepository
    {
        public ExhibitionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Exhibition>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(e => e.TenantID == tenantId);
        }

        public async Task<IReadOnlyList<Exhibition>> GetByStatusAsync(int tenantId, ExhibitionStatus status)
        {
            return await FindAsync(e => e.TenantID == tenantId && e.Status == status);
        }

        public async Task<Exhibition?> GetWithVenueAndSchedulesAsync(int exhibitionId)
        {
            return await _dbSet.AsNoTracking()
                .Include(e => e.Venue)
                .Include(e => e.ExhibitionSchedules)
                .FirstOrDefaultAsync(e => e.ExhibitionID == exhibitionId);
        }

        public async Task<IReadOnlyList<Exhibition>> GetActiveExhibitionsAsync(int tenantId)
        {
            // Active status includes Planning and Open
            return await FindAsync(e => e.TenantID == tenantId && 
                                        (e.Status == ExhibitionStatus.Planning || e.Status == ExhibitionStatus.Open));
        }

        public async Task<IReadOnlyList<Exhibition>> GetUpcomingExhibitionsAsync(int tenantId, int count = 5)
        {
            var now = DateTime.UtcNow;
            return await _dbSet.AsNoTracking()
                .Where(e => e.TenantID == tenantId && e.StartDate > now)
                .OrderBy(e => e.StartDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Exhibition?> GetWithReservationsAndInvoicesAsync(int exhibitionId)
        {
            return await _dbSet.AsNoTracking()
                .Include(e => e.BoothReservations)
                    .ThenInclude(r => r.Invoice)
                .FirstOrDefaultAsync(e => e.ExhibitionID == exhibitionId);
        }
    }
}
