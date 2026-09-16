using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IReservationService
    {
        Task<ServiceResult<PagedResultDto<BoothReservationSummaryDto>>> GetByExhibitionAsync(int tenantId, int exhibitionId, int page, int pageSize);
        Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetByExhibitorAsync(int tenantId, int exhibitorId);
        Task<ServiceResult<BoothReservationDto>> GetByIdAsync(int tenantId, int reservationId);
        Task<ServiceResult<BoothReservationDto>> CreateAsync(int tenantId, string userId, BoothReservationCreateDto dto);
        Task<ServiceResult<BoothReservationDto>> UpdateAsync(int tenantId, int id, BoothReservationUpdateDto dto);
        Task<ServiceResult> CancelAsync(int tenantId, int id);
        Task<ServiceResult<BoothReservationDto>> ApproveAsync(int tenantId, int id);
        Task<ServiceResult<ReservationServiceDto>> AddServiceToReservationAsync(int tenantId, int reservationId, ReservationServiceCreateDto dto);
        Task<ServiceResult> RemoveServiceFromReservationAsync(int tenantId, int reservationId, int rsId);
        Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetUnpaidAsync(int tenantId, int exhibitionId);
    }
}
