using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

public interface IExpenseRepository : IGenericRepository<Expense>
{
    Task<IList<Expense>> GetByExhibitionAsync(int exhibitionId);
    Task<decimal> GetTotalExpensesAsync(int exhibitionId);
}
