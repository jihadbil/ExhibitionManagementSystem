using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface ITenantRepository : IGenericRepository<Tenant>
    {
        Task<Tenant?> GetBySubdomainAsync(string subdomain);
        Task<bool> IsSubdomainUniqueAsync(string subdomain, int? excludeId = null);
        Task<Tenant?> GetWithActiveSubscriptionAsync(int tenantId);
    }
}
