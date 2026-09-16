using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

public class SponsorshipPackageRepository : GenericRepository<SponsorshipPackage>, ISponsorshipPackageRepository
{
    public SponsorshipPackageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SponsorshipPackage>> GetPackagesByExhibitionAsync(int tenantId, int exhibitionId)
    {
        return await _dbSet
            .Include(p => p.Currency)
            .Include(p => p.Contracts)
            .Where(p => p.TenantID == tenantId && p.ExhibitionID == exhibitionId && p.IsActive)
            .OrderBy(p => p.Level)
            .ToListAsync();
    }
}
