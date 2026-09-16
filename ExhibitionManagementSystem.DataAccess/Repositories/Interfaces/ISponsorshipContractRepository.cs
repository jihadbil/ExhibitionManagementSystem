using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

public interface ISponsorshipContractRepository : IGenericRepository<SponsorshipContract>
{
    Task<IReadOnlyList<SponsorshipContract>> GetContractsByExhibitionAsync(int tenantId, int exhibitionId);
    Task<IReadOnlyList<SponsorshipContract>> GetContractsBySponsorAsync(int tenantId, int sponsorId);
    Task<SponsorshipContract?> GetContractWithDetailsAsync(int tenantId, int contractId);
    Task<decimal> GetTotalSponsorshipRevenueAsync(int exhibitionId);
}
