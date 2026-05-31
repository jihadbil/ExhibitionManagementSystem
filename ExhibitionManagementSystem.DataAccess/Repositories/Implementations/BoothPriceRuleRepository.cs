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
    public class BoothPriceRuleRepository : GenericRepository<BoothPriceRule>, IBoothPriceRuleRepository
    {
        public BoothPriceRuleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<BoothPriceRule>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(r => r.TenantID == tenantId);
        }

        public async Task<IReadOnlyList<BoothPriceRule>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(r => r.ExhibitionID == exhibitionId);
        }

        public async Task<BoothPriceRule?> GetApplicableRuleAsync(
            int tenantId, 
            int? exhibitionId, 
            BoothType? boothType, 
            ExhibitorCategory? category, 
            decimal areaSqM, 
            DateTime date)
        {
            var targetDate = date.Date;

            // Fetch candidate rules matching wildcard constraints and validity dates
            var rules = await _dbSet.AsNoTracking()
                .Where(r => r.TenantID == tenantId &&
                            (r.ExhibitionID == null || r.ExhibitionID == exhibitionId) &&
                            (r.BoothType == null || r.BoothType == boothType) &&
                            (r.ExhibitorCategory == null || r.ExhibitorCategory == category) &&
                            (r.MinAreaSqM == null || areaSqM >= r.MinAreaSqM) &&
                            (r.MaxAreaSqM == null || areaSqM <= r.MaxAreaSqM) &&
                            r.ValidFrom <= targetDate &&
                            (r.ValidTo == null || r.ValidTo >= targetDate))
                .ToListAsync();

            if (!rules.Any())
                return null;

            // Order rules: 
            // 1. Specific Exhibition first
            // 2. Specific Booth Type first
            // 3. Specific Exhibitor Category first
            return rules
                .OrderByDescending(r => r.ExhibitionID.HasValue)
                .ThenByDescending(r => r.BoothType.HasValue)
                .ThenByDescending(r => r.ExhibitorCategory.HasValue)
                .FirstOrDefault();
        }
    }
}
