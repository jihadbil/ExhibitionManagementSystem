using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

public class SponsorAdAssignmentRepository : GenericRepository<SponsorAdAssignment>, ISponsorAdAssignmentRepository
{
    public SponsorAdAssignmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SponsorAdAssignment>> GetAssignmentsByContractAsync(int contractId)
    {
        return await _dbSet
            .Include(a => a.Space)
            .Where(a => a.ContractID == contractId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SponsorAdAssignment>> GetAssignmentsBySpaceAsync(int spaceId)
    {
        return await _dbSet
            .Include(a => a.Contract)
                .ThenInclude(c => c.Sponsor)
            .Where(a => a.SpaceID == spaceId)
            .ToListAsync();
    }
}
