using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class VisitorRatingRepository : GenericRepository<VisitorRating>, IVisitorRatingRepository
    {
        public VisitorRatingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<VisitorRating>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(r => r.ExhibitionID == exhibitionId);
        }

        public async Task<IReadOnlyList<VisitorRating>> GetByExhibitorAsync(int exhibitorId)
        {
            return await FindAsync(r => r.ExhibitorID == exhibitorId);
        }

        public async Task<double> GetAverageRatingAsync(int exhibitionId)
        {
            return await _dbSet.AsNoTracking()
                .Where(r => r.ExhibitionID == exhibitionId)
                .AverageAsync(r => (double?)r.Score) ?? 0.0;
        }

        public async Task<bool> HasVisitorRatedAsync(int visitorId, int exhibitionId)
        {
            return await ExistsAsync(r => r.VisitorID == visitorId && r.ExhibitionID == exhibitionId);
        }
    }
}
