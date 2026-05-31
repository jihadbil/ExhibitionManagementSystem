using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IBoothMergeRepository : IGenericRepository<BoothMerge>
    {
        Task<BoothMerge?> GetWithItemsAsync(int mergeId);
        Task<IReadOnlyList<BoothMerge>> GetByExhibitionAsync(int exhibitionId);
        Task<bool> HasActiveReservationAsync(int mergeId);
    }
}
