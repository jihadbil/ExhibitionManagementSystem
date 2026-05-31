using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class CurrencyRepository : GenericRepository<Currency>, ICurrencyRepository
    {
        public CurrencyRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Currency?> GetByCodeAsync(string code)
        {
            return await FirstOrDefaultAsync(c => c.CurrencyCode == code);
        }

        public async Task<bool> IsCodeUniqueAsync(string code, string? excludeCode = null)
        {
            if (excludeCode != null && code.Equals(excludeCode, StringComparison.OrdinalIgnoreCase))
                return true;
            return !await ExistsAsync(c => c.CurrencyCode == code);
        }

        public async Task<IReadOnlyList<Currency>> GetActiveAsync()
        {
            return await FindAsync(c => c.IsActive);
        }
    }
}
