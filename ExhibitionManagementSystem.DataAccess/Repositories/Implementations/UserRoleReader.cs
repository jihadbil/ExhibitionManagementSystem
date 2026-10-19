using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations;

public sealed class UserRoleReader(ApplicationDbContext db) : IUserRoleReader
{
    public async Task<IReadOnlyDictionary<string, List<string>>> ReadAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();
        var rows = await (from membership in db.UserRoles.AsNoTracking()
            join role in db.Roles on membership.RoleId equals role.Id
            where ids.Contains(membership.UserId) && role.Name != null
            select new { membership.UserId, Name = role.Name! }).ToListAsync(cancellationToken);
        return rows.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.Select(r => r.Name).Order().ToList());
    }
}
