using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface ICurrencyRepository : IGenericRepository<Currency>
    {
        Task<Currency?> GetByCodeAsync(string code);
        Task<bool> IsCodeUniqueAsync(string code, string? excludeCode = null);
        Task<IReadOnlyList<Currency>> GetActiveAsync();
    }
}
