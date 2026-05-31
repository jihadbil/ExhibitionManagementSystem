using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IVenueRepository : IGenericRepository<Venue>
    {
        Task<IReadOnlyList<Venue>> GetByTenantAsync(int tenantId);
        Task<Venue?> GetWithHallsAsync(int venueId);
        Task<IReadOnlyList<Venue>> GetActiveVenuesAsync(int tenantId);
    }
}
