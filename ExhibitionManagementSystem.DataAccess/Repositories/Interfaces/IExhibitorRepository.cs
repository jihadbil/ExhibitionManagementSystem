using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IExhibitorRepository : IGenericRepository<Exhibitor>
    {
        Task<IReadOnlyList<Exhibitor>> GetByTenantAsync(int tenantId);
        Task<Exhibitor?> GetByUserIdAsync(string userId);
        Task<IReadOnlyList<Exhibitor>> GetByCategoryAsync(int tenantId, ExhibitorCategory category);
        Task<Exhibitor?> GetWithReservationsAsync(int exhibitorId);
        Task<bool> ExistsForUserAsync(string userId);
        Task<IReadOnlyList<Exhibitor>> SearchAsync(int tenantId, string searchTerm);
    }
}
