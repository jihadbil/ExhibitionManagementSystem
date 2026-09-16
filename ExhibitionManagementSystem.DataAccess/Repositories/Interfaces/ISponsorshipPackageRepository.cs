using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

public interface ISponsorshipPackageRepository : IGenericRepository<SponsorshipPackage>
{
    Task<IReadOnlyList<SponsorshipPackage>> GetPackagesByExhibitionAsync(int tenantId, int exhibitionId);
}
