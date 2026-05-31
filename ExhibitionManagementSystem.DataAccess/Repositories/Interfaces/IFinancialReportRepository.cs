using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IFinancialReportRepository : IGenericRepository<FinancialReport>
    {
        Task<IReadOnlyList<FinancialReport>> GetByTenantAsync(int tenantId);
        Task<IReadOnlyList<FinancialReport>> GetByExhibitionAsync(int exhibitionId);
        Task<FinancialReport?> GetLatestReportAsync(int tenantId);
    }
}
