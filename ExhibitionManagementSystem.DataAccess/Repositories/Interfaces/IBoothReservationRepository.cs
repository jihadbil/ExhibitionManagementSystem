using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IBoothReservationRepository : IGenericRepository<BoothReservation>
    {
        Task<IReadOnlyList<BoothReservation>> GetByExhibitionAsync(int exhibitionId);
        Task<IReadOnlyList<BoothReservation>> GetByExhibitorAsync(int exhibitorId);
        Task<IReadOnlyList<BoothReservation>> GetByStatusAsync(int exhibitionId, ReservationStatus status);
        Task<BoothReservation?> GetWithInvoiceAsync(int reservationId);
        Task<BoothReservation?> GetWithServicesAsync(int reservationId);
        Task<BoothReservation?> GetFullDetailAsync(int reservationId);
        Task<bool> IsBoothReservedAsync(int boothId, int exhibitionId);
        Task<bool> IsMergeReservedAsync(int mergeId, int exhibitionId);
        Task<decimal> GetTotalRevenueAsync(int exhibitionId);
        Task<IReadOnlyList<BoothReservation>> GetUnpaidReservationsAsync(int exhibitionId);
    }
}
