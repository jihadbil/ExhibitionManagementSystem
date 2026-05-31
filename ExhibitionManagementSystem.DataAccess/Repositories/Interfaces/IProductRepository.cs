using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IReadOnlyList<Product>> GetByExhibitorAsync(int exhibitorId);
        Task<IReadOnlyList<Product>> GetByExhibitionAsync(int exhibitionId);
        Task<IReadOnlyList<Product>> SearchAsync(int tenantId, string searchTerm);
    }
}
