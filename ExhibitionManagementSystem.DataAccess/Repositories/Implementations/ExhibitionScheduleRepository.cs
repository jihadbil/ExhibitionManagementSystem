using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class ExhibitionScheduleRepository : GenericRepository<ExhibitionSchedule>, IExhibitionScheduleRepository
    {
        public ExhibitionScheduleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<ExhibitionSchedule>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(s => s.ExhibitionID == exhibitionId);
        }

        public async Task<IReadOnlyList<ExhibitionSchedule>> GetByHallAsync(int hallId)
        {
            return await FindAsync(s => s.HallID == hallId);
        }

        public async Task<IReadOnlyList<ExhibitionSchedule>> GetByDateRangeAsync(int exhibitionId, DateTime from, DateTime to)
        {
            return await FindAsync(s => s.ExhibitionID == exhibitionId && s.StartDateTime >= from && s.EndDateTime <= to);
        }
    }
}
