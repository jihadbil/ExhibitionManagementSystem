using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Product>> GetByExhibitorAsync(int exhibitorId)
        {
            return await FindAsync(p => p.ExhibitorID == exhibitorId);
        }

        public async Task<IReadOnlyList<Product>> GetByExhibitionAsync(int exhibitionId)
        {
            return await FindAsync(p => p.ExhibitionID == exhibitionId);
        }

        public async Task<IReadOnlyList<Product>> SearchAsync(int tenantId, string searchTerm)
        {
            var query = _dbSet.AsNoTracking()
                .Include(p => p.Exhibitor)
                .Where(p => p.Exhibitor.TenantID == tenantId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) ||
                                         p.Category.Contains(searchTerm) ||
                                         p.Description.Contains(searchTerm));
            }

            return await query.ToListAsync();
        }
    }
}
