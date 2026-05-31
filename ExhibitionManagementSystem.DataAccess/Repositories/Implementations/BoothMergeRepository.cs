using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class BoothMergeRepository : GenericRepository<BoothMerge>, IBoothMergeRepository
    {
        public BoothMergeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<BoothMerge?> GetWithItemsAsync(int mergeId)
        {
            return await _dbSet.AsNoTracking()
                .Include(bm => bm.MergeItems)
                    .ThenInclude(bmi => bmi.Booth)
                .FirstOrDefaultAsync(bm => bm.MergeID == mergeId);
        }

        public async Task<IReadOnlyList<BoothMerge>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(bm => bm.ExhibitionID == exhibitionId);
        }

        public async Task<bool> HasActiveReservationAsync(int mergeId)
        {
            var merge = await _dbSet.AsNoTracking()
                .Include(bm => bm.Reservation)
                .FirstOrDefaultAsync(bm => bm.MergeID == mergeId);

            if (merge == null || merge.ReservationID == null || merge.Reservation == null)
                return false;

            return merge.Reservation.Status != ReservationStatus.Cancelled;
        }
    }
}
