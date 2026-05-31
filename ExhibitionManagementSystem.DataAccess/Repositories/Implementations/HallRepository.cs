using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class HallRepository : GenericRepository<Hall>, IHallRepository
    {
        public HallRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Hall>> GetByVenueAsync(int venueId)
        {
            return await FindAsync(h => h.VenueID == venueId);
        }

        public async Task<Hall?> GetWithBoothsAsync(int hallId)
        {
            return await _dbSet.AsNoTracking()
                .Include(h => h.Booths)
                .FirstOrDefaultAsync(h => h.HallID == hallId);
        }

        public async Task<IReadOnlyList<Hall>> GetAvailableHallsAsync(int venueId)
        {
            return await FindAsync(h => h.VenueID == venueId && h.IsActive);
        }
    }
}
