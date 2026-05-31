using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IBoothService
    {
        Task<ServiceResult<IList<BoothDto>>> GetByHallAsync(int tenantId, int hallId);
        Task<ServiceResult<IList<BoothSummaryDto>>> GetAvailableAsync(int tenantId, int hallId, int exhibitionId);
        Task<ServiceResult<BoothDto>> GetByIdAsync(int tenantId, int boothId);
        Task<ServiceResult<BoothDto>> CreateAsync(int tenantId, BoothCreateDto dto);
        Task<ServiceResult<BoothDto>> UpdateAsync(int tenantId, int boothId, BoothUpdateDto dto);
        Task<ServiceResult<BoothMergeDto>> MergeBoothsAsync(int tenantId, BoothMergeCreateDto dto);
        Task<ServiceResult> UnmergeBoothsAsync(int tenantId, int mergeId);
    }
}
