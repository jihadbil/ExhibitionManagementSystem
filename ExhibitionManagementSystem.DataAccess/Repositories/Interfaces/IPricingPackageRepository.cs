using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IPricingPackageRepository : IGenericRepository<PricingPackage>
    {
        Task<IReadOnlyList<PricingPackage>> GetByTenantAsync(int tenantId);
        Task<IReadOnlyList<PricingPackage>> GetActivePackagesAsync(int tenantId);
        Task<PricingPackage?> GetWithServicesAsync(int packageId);
    }
}
