using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IBoothPriceRuleRepository : IGenericRepository<BoothPriceRule>
    {
        Task<IReadOnlyList<BoothPriceRule>> GetByTenantAsync(int tenantId);
        Task<IReadOnlyList<BoothPriceRule>> GetByExhibitionAsync(int exhibitionId);
        Task<BoothPriceRule?> GetApplicableRuleAsync(
            int tenantId, 
            int? exhibitionId, 
            BoothType? boothType, 
            ExhibitorCategory? category, 
            decimal areaSqM, 
            DateTime date);
    }
}
