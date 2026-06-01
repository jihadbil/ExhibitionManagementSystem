using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
{
    public ExpenseRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IList<Expense>> GetByExhibitionAsync(int exhibitionId)
        => await _dbSet.Where(e => e.ExhibitionID == exhibitionId).ToListAsync();

    public async Task<decimal> GetTotalExpensesAsync(int exhibitionId)
        => await _dbSet.Where(e => e.ExhibitionID == exhibitionId)
                       .SumAsync(e => e.Amount);
}
