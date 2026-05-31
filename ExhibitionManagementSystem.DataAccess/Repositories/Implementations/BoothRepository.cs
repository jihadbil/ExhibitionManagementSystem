using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class BoothRepository : GenericRepository<Booth>, IBoothRepository
    {
        public BoothRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Booth>> GetByHallAsync(int hallId)
        {
            return await FindAsync(b => b.HallID == hallId);
        }

        public async Task<IReadOnlyList<Booth>> GetAvailableBoothsAsync(int hallId)
        {
            return await FindAsync(b => b.HallID == hallId && b.Status == BoothStatus.Available && !b.IsMerged);
        }

        public async Task<IReadOnlyList<Booth>> GetByStatusAsync(int hallId, BoothStatus status)
        {
            return await FindAsync(b => b.HallID == hallId && b.Status == status);
        }

        public async Task<Booth?> GetWithMergeInfoAsync(int boothId)
        {
            return await _dbSet.AsNoTracking()
                .Include(b => b.BoothMerge)
                .FirstOrDefaultAsync(b => b.BoothID == boothId);
        }

        public async Task<IReadOnlyList<Booth>> GetBoothsForFloorPlanAsync(int hallId)
        {
            // For the floor plan we retrieve all non-deleted booths in the hall
            return await FindAsync(b => b.HallID == hallId);
        }
    }
}
