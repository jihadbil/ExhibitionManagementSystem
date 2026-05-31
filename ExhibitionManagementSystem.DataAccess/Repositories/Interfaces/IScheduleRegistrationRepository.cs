using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IScheduleRegistrationRepository : IGenericRepository<ScheduleRegistration>
    {
        Task<IReadOnlyList<ScheduleRegistration>> GetByScheduleAsync(int scheduleId);
        Task<IReadOnlyList<ScheduleRegistration>> GetByVisitorAsync(int visitorId);
        Task<bool> IsVisitorRegisteredAsync(int scheduleId, int visitorId);
        Task<int> GetRegistrationCountAsync(int scheduleId);
    }
}
