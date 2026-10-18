using Microsoft.Data.SqlClient;

namespace ExhibitionManagementSystem.Services.Common;

internal static class DatabaseConflict
{
    public static bool IsLockConflict(Exception exception)
    {
        // EF's SQL execution strategy may wrap a deadlock in InvalidOperationException.
        for (Exception? current = exception; current != null; current = current.InnerException)
            if (current is SqlException sql && sql.Number is 1205 or 1222)
                return true;
        return false;
    }
}
