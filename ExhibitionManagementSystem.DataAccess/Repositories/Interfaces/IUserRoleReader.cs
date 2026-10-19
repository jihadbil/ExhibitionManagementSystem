namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

public interface IUserRoleReader
{
    Task<IReadOnlyDictionary<string, List<string>>> ReadAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}
