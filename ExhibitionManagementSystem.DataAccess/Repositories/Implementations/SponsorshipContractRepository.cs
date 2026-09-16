using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

public class SponsorshipContractRepository : GenericRepository<SponsorshipContract>, ISponsorshipContractRepository
{
    public SponsorshipContractRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SponsorshipContract>> GetContractsByExhibitionAsync(int tenantId, int exhibitionId)
    {
        return await _dbSet
            .Include(c => c.Sponsor)
            .Include(c => c.Package)
            .Include(c => c.Currency)
            .Include(c => c.AdAssignments)
                .ThenInclude(a => a.Space)
            .Where(c => c.TenantID == tenantId && c.ExhibitionID == exhibitionId)
            .OrderByDescending(c => c.SignedDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SponsorshipContract>> GetContractsBySponsorAsync(int tenantId, int sponsorId)
    {
        return await _dbSet
            .Include(c => c.Exhibition)
            .Include(c => c.Package)
            .Include(c => c.Currency)
            .Where(c => c.TenantID == tenantId && c.SponsorID == sponsorId)
            .OrderByDescending(c => c.SignedDate)
            .ToListAsync();
    }

    public async Task<SponsorshipContract?> GetContractWithDetailsAsync(int tenantId, int contractId)
    {
        return await _dbSet
            .Include(c => c.Sponsor)
            .Include(c => c.Exhibition)
            .Include(c => c.Package)
            .Include(c => c.Currency)
            .Include(c => c.AdAssignments)
                .ThenInclude(a => a.Space)
            .FirstOrDefaultAsync(c => c.TenantID == tenantId && c.ContractID == contractId);
    }

    public async Task<decimal> GetTotalSponsorshipRevenueAsync(int exhibitionId)
    {
        return await _dbSet
            .Where(c => c.ExhibitionID == exhibitionId && c.Status != SponsorshipStatus.Cancelled)
            .SumAsync(c => c.TotalAmount);
    }
}
