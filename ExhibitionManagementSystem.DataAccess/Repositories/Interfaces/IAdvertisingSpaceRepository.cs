using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

public interface IAdvertisingSpaceRepository : IGenericRepository<AdvertisingSpace>
{
    Task<IReadOnlyList<AdvertisingSpace>> GetSpacesByExhibitionAsync(int tenantId, int exhibitionId, bool? availableOnly = null);
}
