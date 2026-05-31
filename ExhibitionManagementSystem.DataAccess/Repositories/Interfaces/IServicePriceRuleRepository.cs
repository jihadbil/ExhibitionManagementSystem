using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IServicePriceRuleRepository : IGenericRepository<ServicePriceRule>
    {
        Task<IReadOnlyList<ServicePriceRule>> GetByServiceAsync(int serviceId);
        Task<IReadOnlyList<ServicePriceRule>> GetByExhibitionAsync(int exhibitionId);
        Task<ServicePriceRule?> GetApplicableRuleAsync(
            int serviceId, 
            int? exhibitionId, 
            ExhibitorCategory? category, 
            DateTime date);
    }
}
