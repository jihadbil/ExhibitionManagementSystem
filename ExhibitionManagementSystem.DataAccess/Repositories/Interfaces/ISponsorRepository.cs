using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

public interface ISponsorRepository : IGenericRepository<Sponsor>
{
    Task<IReadOnlyList<Sponsor>> GetActiveSponsorsAsync(int tenantId);
    Task<Sponsor?> GetSponsorWithContractsAsync(int tenantId, int sponsorId);
}
