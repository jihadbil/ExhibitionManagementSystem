using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.DataAccess;

public static class DatabaseConflicts
{
    public static bool IsConflict(Exception exception)
    {
        // EF's SQL execution strategy can wrap DbUpdateException in another
        // InvalidOperationException, so inspecting only InnerException misses deadlocks.
        for (Exception? cause = exception; cause != null; cause = cause.InnerException)
            if (cause is DbUpdateConcurrencyException || cause is SqlException { Number: 2601 or 2627 or 1205 })
                return true;
        return false;
    }
}
