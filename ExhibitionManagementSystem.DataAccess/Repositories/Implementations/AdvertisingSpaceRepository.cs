using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

public class AdvertisingSpaceRepository : GenericRepository<AdvertisingSpace>, IAdvertisingSpaceRepository
{
    public AdvertisingSpaceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AdvertisingSpace>> GetSpacesByExhibitionAsync(int tenantId, int exhibitionId, bool? availableOnly = null)
    {
        var query = _dbSet
            .Include(a => a.Venue)
            .Include(a => a.Hall)
            .Include(a => a.Currency)
            .Include(a => a.AdAssignments)
                .ThenInclude(aa => aa.Contract)
                    .ThenInclude(c => c.Sponsor)
            .Where(a => a.TenantID == tenantId && a.ExhibitionID == exhibitionId);

        if (availableOnly.HasValue)
        {
            query = query.Where(a => a.IsAvailable == availableOnly.Value);
        }

        return await query.OrderBy(a => a.SpaceCode).ToListAsync();
    }
}
