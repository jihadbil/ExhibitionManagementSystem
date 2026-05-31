using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class PricingPackageRepository : GenericRepository<PricingPackage>, IPricingPackageRepository
    {
        public PricingPackageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<PricingPackage>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(p => p.TenantID == tenantId && p.IsActive);
        }

        public async Task<IReadOnlyList<PricingPackage>> GetActivePackagesAsync(int tenantId)
        {
            var today = DateTime.UtcNow.Date;
            return await FindAsync(p => p.TenantID == tenantId &&
                                        p.IsActive &&
                                        p.ValidFrom <= today &&
                                        (p.ValidTo == null || p.ValidTo >= today));
        }

        public async Task<PricingPackage?> GetWithServicesAsync(int packageId)
        {
            return await _dbSet.AsNoTracking()
                .Include(p => p.PackageServices)
                    .ThenInclude(ps => ps.Service)
                .FirstOrDefaultAsync(p => p.PackageID == packageId);
        }
    }
}
