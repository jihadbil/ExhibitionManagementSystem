using ExhibitionManagementSystem.DataAccess;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExhibitionManagementSystem.Health;

public sealed class DatabaseHealthCheck(
    ApplicationDbContext dbContext,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database is unavailable.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database readiness check failed.");
            return HealthCheckResult.Unhealthy("Database is unavailable.");
        }
    }
}
