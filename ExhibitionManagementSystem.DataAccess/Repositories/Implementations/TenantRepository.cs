using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class TenantRepository : GenericRepository<Tenant>, ITenantRepository
    {
        public TenantRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Tenant?> GetBySubdomainAsync(string subdomain)
        {
            return await FirstOrDefaultAsync(t => t.Subdomain == subdomain);
        }

        public async Task<bool> IsSubdomainUniqueAsync(string subdomain, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return !await ExistsAsync(t => t.Subdomain == subdomain && t.TenantID != excludeId.Value);
            }
            return !await ExistsAsync(t => t.Subdomain == subdomain);
        }

        public async Task<Tenant?> GetWithActiveSubscriptionAsync(int tenantId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.AsNoTracking()
                .Include(t => t.TenantSubscriptions)
                .FirstOrDefaultAsync(t => t.TenantID == tenantId &&
                    t.TenantSubscriptions.Any(s => (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial) &&
                                                    s.StartDate <= today &&
                                                    s.EndDate >= today));
        }
    }
}
