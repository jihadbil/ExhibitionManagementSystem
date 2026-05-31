using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IBoothRepository : IGenericRepository<Booth>
    {
        Task<IReadOnlyList<Booth>> GetByHallAsync(int hallId);
        Task<IReadOnlyList<Booth>> GetAvailableBoothsAsync(int hallId);
        Task<IReadOnlyList<Booth>> GetByStatusAsync(int hallId, BoothStatus status);
        Task<Booth?> GetWithMergeInfoAsync(int boothId);
        Task<IReadOnlyList<Booth>> GetBoothsForFloorPlanAsync(int hallId);
    }
}
