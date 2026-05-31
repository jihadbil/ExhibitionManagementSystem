using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class TenantSubscriptionRepository : GenericRepository<TenantSubscription>, ITenantSubscriptionRepository
    {
        public TenantSubscriptionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<TenantSubscription?> GetActiveSubscriptionAsync(int tenantId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantID == tenantId &&
                                          (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial) &&
                                          s.StartDate <= today &&
                                          s.EndDate >= today);
        }

        public async Task<IReadOnlyList<TenantSubscription>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(s => s.TenantID == tenantId);
        }

        public async Task<IReadOnlyList<TenantSubscription>> GetExpiringSubscriptionsAsync(int daysAhead)
        {
            var today = DateTime.UtcNow.Date;
            var maxDate = today.AddDays(daysAhead);

            return await FindAsync(s => (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial) &&
                                         s.EndDate <= maxDate &&
                                         s.EndDate >= today);
        }
    }
}
