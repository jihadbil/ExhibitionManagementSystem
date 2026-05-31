using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IBoothStaffRepository : IGenericRepository<BoothStaff>
    {
        Task<IReadOnlyList<BoothStaff>> GetByReservationAsync(int reservationId);
        Task<IReadOnlyList<BoothStaff>> GetByExhibitorAsync(int exhibitorId);
    }
}
