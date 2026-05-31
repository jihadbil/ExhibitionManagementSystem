using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class ExhibitorRepository : GenericRepository<Exhibitor>, IExhibitorRepository
    {
        public ExhibitorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Exhibitor>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(e => e.TenantID == tenantId);
        }

        public async Task<Exhibitor?> GetByUserIdAsync(string userId)
        {
            return await FirstOrDefaultAsync(e => e.UserId == userId);
        }

        public async Task<IReadOnlyList<Exhibitor>> GetByCategoryAsync(int tenantId, ExhibitorCategory category)
        {
            return await FindAsync(e => e.TenantID == tenantId && e.ExhibitorCategory == category);
        }

        public async Task<Exhibitor?> GetWithReservationsAsync(int exhibitorId)
        {
            return await _dbSet.AsNoTracking()
                .Include(e => e.BoothReservations)
                .FirstOrDefaultAsync(e => e.ExhibitorID == exhibitorId);
        }

        public async Task<bool> ExistsForUserAsync(string userId)
        {
            return await ExistsAsync(e => e.UserId == userId);
        }

        public async Task<IReadOnlyList<Exhibitor>> SearchAsync(int tenantId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await FindAsync(e => e.TenantID == tenantId);
            }

            return await FindAsync(e => e.TenantID == tenantId && 
                                        (e.CompanyName.Contains(searchTerm) || 
                                         e.ContactPerson.Contains(searchTerm) || 
                                         e.Email.Contains(searchTerm)));
        }
    }
}
