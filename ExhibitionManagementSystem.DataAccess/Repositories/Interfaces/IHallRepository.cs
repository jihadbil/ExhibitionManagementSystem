using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IHallRepository : IGenericRepository<Hall>
    {
        Task<IReadOnlyList<Hall>> GetByVenueAsync(int venueId);
        Task<Hall?> GetWithBoothsAsync(int hallId);
        Task<IReadOnlyList<Hall>> GetAvailableHallsAsync(int venueId);
    }
}
