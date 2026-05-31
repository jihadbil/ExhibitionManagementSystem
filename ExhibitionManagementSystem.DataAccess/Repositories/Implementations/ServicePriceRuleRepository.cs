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
    public class ServicePriceRuleRepository : GenericRepository<ServicePriceRule>, IServicePriceRuleRepository
    {
        public ServicePriceRuleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<ServicePriceRule>> GetByServiceAsync(int serviceId)
        {
            return await FindAsync(r => r.ServiceID == serviceId);
        }

        public async Task<IReadOnlyList<ServicePriceRule>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(r => r.ExhibitionID == exhibitionId);
        }

        public async Task<ServicePriceRule?> GetApplicableRuleAsync(
            int serviceId, 
            int? exhibitionId, 
            ExhibitorCategory? category, 
            DateTime date)
        {
            var targetDate = date.Date;

            // Fetch candidate rules matching wildcard constraints and validity dates
            var rules = await _dbSet.AsNoTracking()
                .Where(r => r.ServiceID == serviceId &&
                            (r.ExhibitionID == null || r.ExhibitionID == exhibitionId) &&
                            (r.ExhibitorCategory == null || r.ExhibitorCategory == category) &&
                            r.ValidFrom <= targetDate &&
                            (r.ValidTo == null || r.ValidTo >= targetDate))
                .ToListAsync();

            if (!rules.Any())
                return null;

            // Order rules:
            // 1. Specific Exhibition first
            // 2. Specific Exhibitor Category first
            return rules
                .OrderByDescending(r => r.ExhibitionID.HasValue)
                .ThenByDescending(r => r.ExhibitorCategory.HasValue)
                .FirstOrDefault();
        }
    }
}
