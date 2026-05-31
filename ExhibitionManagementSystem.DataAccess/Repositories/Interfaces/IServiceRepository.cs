using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IServiceRepository : IGenericRepository<Service>
    {
        Task<IReadOnlyList<Service>> GetByTenantAsync(int tenantId);
        Task<IReadOnlyList<Service>> GetMandatoryServicesAsync(int tenantId);
        Task<IReadOnlyList<Service>> GetByCategoryAsync(int tenantId, string category);
        Task<Service?> GetWithPriceRulesAsync(int serviceId);
    }
}
