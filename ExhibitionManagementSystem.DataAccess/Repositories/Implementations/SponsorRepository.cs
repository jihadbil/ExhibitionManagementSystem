using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

public class SponsorRepository : GenericRepository<Sponsor>, ISponsorRepository
{
    public SponsorRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Sponsor>> GetActiveSponsorsAsync(int tenantId)
    {
        return await _dbSet
            .Where(s => s.TenantID == tenantId && s.IsActive)
            .OrderBy(s => s.CompanyName)
            .ToListAsync();
    }

    public async Task<Sponsor?> GetSponsorWithContractsAsync(int tenantId, int sponsorId)
    {
        return await _dbSet
            .Include(s => s.Contracts)
                .ThenInclude(c => c.Exhibition)
            .Include(s => s.Contracts)
                .ThenInclude(c => c.Package)
            .FirstOrDefaultAsync(s => s.TenantID == tenantId && s.SponsorID == sponsorId);
    }
}
