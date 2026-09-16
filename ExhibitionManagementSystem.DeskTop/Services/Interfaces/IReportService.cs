using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IReportService
    {
        Task<ServiceResult<FinancialReportDto>> GenerateExhibitionReportAsync(int tenantId, int exhibitionId, string userId);
        Task<ServiceResult<FinancialReportDto>> GetReportByIdAsync(int tenantId, int reportId);
    }
}
