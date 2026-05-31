using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class ServiceRepository : GenericRepository<Service>, IServiceRepository
    {
        public ServiceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Service>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(s => s.TenantID == tenantId && s.IsActive);
        }

        public async Task<IReadOnlyList<Service>> GetMandatoryServicesAsync(int tenantId)
        {
            return await FindAsync(s => s.TenantID == tenantId && s.IsMandatory && s.IsActive);
        }

        public async Task<IReadOnlyList<Service>> GetByCategoryAsync(int tenantId, string category)
        {
            return await FindAsync(s => s.TenantID == tenantId && s.Category == category && s.IsActive);
        }

        public async Task<Service?> GetWithPriceRulesAsync(int serviceId)
        {
            // Since there is no collection property on Service for PriceRules in the model,
            // we retrieve the service by ID.
            return await GetByIdAsync(serviceId);
        }
    }
}
