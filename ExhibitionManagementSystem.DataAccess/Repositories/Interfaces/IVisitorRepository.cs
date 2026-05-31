using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IVisitorRepository : IGenericRepository<Visitor>
    {
        Task<IReadOnlyList<Visitor>> GetByTenantAsync(int tenantId);
        Task<Visitor?> GetByUserIdAsync(string userId);
        Task<Visitor?> GetByEmailAsync(int tenantId, string email);
        Task<IReadOnlyList<Visitor>> SearchAsync(int tenantId, string searchTerm);
        Task<Visitor?> GetWithTicketsAsync(int visitorId);
    }
}
