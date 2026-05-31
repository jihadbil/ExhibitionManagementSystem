using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class VenueRepository : GenericRepository<Venue>, IVenueRepository
    {
        public VenueRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Venue>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(v => v.TenantID == tenantId);
        }

        public async Task<Venue?> GetWithHallsAsync(int venueId)
        {
            return await _dbSet.AsNoTracking()
                .Include(v => v.Halls)
                .FirstOrDefaultAsync(v => v.VenueID == venueId);
        }

        public async Task<IReadOnlyList<Venue>> GetActiveVenuesAsync(int tenantId)
        {
            return await FindAsync(v => v.TenantID == tenantId && v.IsActive);
        }
    }
}
