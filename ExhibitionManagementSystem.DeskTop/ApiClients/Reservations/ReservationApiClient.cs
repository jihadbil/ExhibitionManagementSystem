using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Reservations;

public class ReservationApiClient : ApiClientBase, IReservationService
{
    public ReservationApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<PagedResultDto<BoothReservationSummaryDto>>> GetByExhibitionAsync(int tenantId, int exhibitionId, int page, int pageSize)
    {
        return GetAsync<PagedResultDto<BoothReservationSummaryDto>>($"api/reservations/exhibition/{exhibitionId}?page={page}&pageSize={pageSize}");
    }

    public Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetByExhibitorAsync(int tenantId, int exhibitorId)
    {
        return GetAsync<IList<BoothReservationSummaryDto>>($"api/reservations/exhibitor/{exhibitorId}");
    }

    public Task<ServiceResult<BoothReservationDto>> GetByIdAsync(int tenantId, int reservationId)
    {
        return GetAsync<BoothReservationDto>($"api/reservations/{reservationId}");
    }

    public Task<ServiceResult<BoothReservationDto>> CreateAsync(int tenantId, string userId, BoothReservationCreateDto dto)
    {
        return PostAsync<BoothReservationDto>("api/reservations", dto);
    }

    public Task<ServiceResult<BoothReservationDto>> UpdateAsync(int tenantId, int id, BoothReservationUpdateDto dto)
    {
        return PutAsync<BoothReservationDto>($"api/reservations/{id}", dto);
    }

    public Task<ServiceResult> CancelAsync(int tenantId, int id)
    {
        return PatchVoidAsync($"api/reservations/{id}/cancel");
    }

    public Task<ServiceResult<BoothReservationDto>> ApproveAsync(int tenantId, int id)
    {
        return PatchAsync<BoothReservationDto>($"api/reservations/{id}/approve");
    }

    public Task<ServiceResult<ReservationServiceDto>> AddServiceToReservationAsync(int tenantId, int reservationId, ReservationServiceCreateDto dto)
    {
        return PostAsync<ReservationServiceDto>($"api/reservations/{reservationId}/services", dto);
    }

    public Task<ServiceResult> RemoveServiceFromReservationAsync(int tenantId, int reservationId, int rsId)
    {
        return DeleteAsync($"api/reservations/{reservationId}/services/{rsId}");
    }

    public Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetUnpaidAsync(int tenantId, int exhibitionId)
    {
        return GetAsync<IList<BoothReservationSummaryDto>>($"api/reservations/exhibition/{exhibitionId}/unpaid");
    }
}
