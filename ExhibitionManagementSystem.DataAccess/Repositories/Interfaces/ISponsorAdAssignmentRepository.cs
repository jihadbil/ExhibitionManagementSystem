using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

public interface ISponsorAdAssignmentRepository : IGenericRepository<SponsorAdAssignment>
{
    Task<IReadOnlyList<SponsorAdAssignment>> GetAssignmentsByContractAsync(int contractId);
    Task<IReadOnlyList<SponsorAdAssignment>> GetAssignmentsBySpaceAsync(int spaceId);
}
