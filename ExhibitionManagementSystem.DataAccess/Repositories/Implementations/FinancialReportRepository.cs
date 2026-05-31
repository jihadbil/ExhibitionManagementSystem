using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class FinancialReportRepository : GenericRepository<FinancialReport>, IFinancialReportRepository
    {
        public FinancialReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<FinancialReport>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(r => r.TenantID == tenantId);
        }

        public async Task<IReadOnlyList<FinancialReport>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(r => r.ExhibitionID == exhibitionId);
        }

        public async Task<FinancialReport?> GetLatestReportAsync(int tenantId)
        {
            return await _dbSet.AsNoTracking()
                .Where(r => r.TenantID == tenantId)
                .OrderByDescending(r => r.GeneratedAt)
                .FirstOrDefaultAsync();
        }
    }
}
