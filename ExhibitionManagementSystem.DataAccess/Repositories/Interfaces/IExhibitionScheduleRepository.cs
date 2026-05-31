using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IExhibitionScheduleRepository : IGenericRepository<ExhibitionSchedule>
    {
        Task<IReadOnlyList<ExhibitionSchedule>> GetByExhibitionAsync(int exhibitionId);
        Task<IReadOnlyList<ExhibitionSchedule>> GetByHallAsync(int hallId);
        Task<IReadOnlyList<ExhibitionSchedule>> GetByDateRangeAsync(int exhibitionId, DateTime from, DateTime to);
    }
}
