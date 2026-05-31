using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IVisitorRatingRepository : IGenericRepository<VisitorRating>
    {
        Task<IReadOnlyList<VisitorRating>> GetByExhibitionAsync(int exhibitionId);
        Task<IReadOnlyList<VisitorRating>> GetByExhibitorAsync(int exhibitorId);
        Task<double> GetAverageRatingAsync(int exhibitionId);
        Task<bool> HasVisitorRatedAsync(int visitorId, int exhibitionId);
    }
}
