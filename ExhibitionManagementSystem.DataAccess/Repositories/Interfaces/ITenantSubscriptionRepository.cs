using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface ITenantSubscriptionRepository : IGenericRepository<TenantSubscription>
    {
        Task<TenantSubscription?> GetActiveSubscriptionAsync(int tenantId);
        Task<IReadOnlyList<TenantSubscription>> GetByTenantAsync(int tenantId);
        Task<IReadOnlyList<TenantSubscription>> GetExpiringSubscriptionsAsync(int daysAhead);
    }
}
