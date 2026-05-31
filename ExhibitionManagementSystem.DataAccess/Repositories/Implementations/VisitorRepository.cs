using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class VisitorRepository : GenericRepository<Visitor>, IVisitorRepository
    {
        public VisitorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Visitor>> GetByTenantAsync(int tenantId)
        {
            return await FindAsync(v => v.TenantID == tenantId);
        }

        public async Task<Visitor?> GetByUserIdAsync(string userId)
        {
            return await FirstOrDefaultAsync(v => v.UserId == userId);
        }

        public async Task<Visitor?> GetByEmailAsync(int tenantId, string email)
        {
            return await FirstOrDefaultAsync(v => v.TenantID == tenantId && v.Email == email);
        }

        public async Task<IReadOnlyList<Visitor>> SearchAsync(int tenantId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await FindAsync(v => v.TenantID == tenantId);
            }

            return await FindAsync(v => v.TenantID == tenantId && 
                                        (v.FullName.Contains(searchTerm) || 
                                         v.Email.Contains(searchTerm) || 
                                         v.Phone.Contains(searchTerm)));
        }

        public async Task<Visitor?> GetWithTicketsAsync(int visitorId)
        {
            return await _dbSet.AsNoTracking()
                .Include(v => v.Tickets)
                .FirstOrDefaultAsync(v => v.VisitorID == visitorId);
        }
    }
}
