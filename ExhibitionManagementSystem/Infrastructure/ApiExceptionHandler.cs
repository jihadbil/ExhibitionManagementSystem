using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
            return true;
        }
        for (Exception? cause = exception; cause != null; cause = cause.InnerException)
        {
            if (cause is not System.ComponentModel.DataAnnotations.ValidationException) continue;
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 400, Title = cause.Message }, cancellationToken);
            return true;
        }
        logger.LogError(exception, "Request {TraceId} failed", context.TraceIdentifier);
        var status = ExhibitionManagementSystem.DataAccess.DatabaseConflicts.IsConflict(exception) ? 409 : 500;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status == 409 ? "The resource changed. Reload before retrying." : "The request could not be completed.",
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, cancellationToken);
        return true;
    }
}
