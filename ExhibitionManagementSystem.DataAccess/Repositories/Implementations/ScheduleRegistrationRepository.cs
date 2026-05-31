using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class ScheduleRegistrationRepository : GenericRepository<ScheduleRegistration>, IScheduleRegistrationRepository
    {
        public ScheduleRegistrationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<ScheduleRegistration>> GetByScheduleAsync(int scheduleId)
        {
            return await FindAsync(r => r.ScheduleID == scheduleId);
        }

        public async Task<IReadOnlyList<ScheduleRegistration>> GetByVisitorAsync(int visitorId)
        {
            return await FindAsync(r => r.VisitorID == visitorId);
        }

        public async Task<bool> IsVisitorRegisteredAsync(int scheduleId, int visitorId)
        {
            return await ExistsAsync(r => r.ScheduleID == scheduleId && r.VisitorID == visitorId);
        }

        public async Task<int> GetRegistrationCountAsync(int scheduleId)
        {
            return await CountAsync(r => r.ScheduleID == scheduleId);
        }
    }
}
