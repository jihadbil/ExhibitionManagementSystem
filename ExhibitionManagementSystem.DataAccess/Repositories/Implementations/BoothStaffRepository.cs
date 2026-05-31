using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class BoothStaffRepository : GenericRepository<BoothStaff>, IBoothStaffRepository
    {
        public BoothStaffRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<BoothStaff>> GetByReservationAsync(int reservationId)
        {
            return await FindAsync(s => s.ReservationID == reservationId);
        }

        public async Task<IReadOnlyList<BoothStaff>> GetByExhibitorAsync(int exhibitorId)
        {
            return await _dbSet.AsNoTracking()
                .Include(s => s.Reservation)
                .Where(s => s.Reservation.ExhibitorID == exhibitorId)
                .ToListAsync();
        }
    }
}
