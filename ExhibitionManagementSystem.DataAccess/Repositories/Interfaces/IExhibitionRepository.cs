using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IExhibitionRepository : IGenericRepository<Exhibition>
    {
        Task<IReadOnlyList<Exhibition>> GetByTenantAsync(int tenantId);
        Task<IReadOnlyList<Exhibition>> GetByStatusAsync(int tenantId, ExhibitionStatus status);
        Task<Exhibition?> GetWithVenueAndSchedulesAsync(int exhibitionId);
        Task<IReadOnlyList<Exhibition>> GetActiveExhibitionsAsync(int tenantId);
        Task<IReadOnlyList<Exhibition>> GetUpcomingExhibitionsAsync(int tenantId, int count = 5);
        Task<Exhibition?> GetWithReservationsAndInvoicesAsync(int exhibitionId);
    }
}
