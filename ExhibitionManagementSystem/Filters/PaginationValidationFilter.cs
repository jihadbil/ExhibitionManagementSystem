using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ExhibitionManagementSystem.Filters;

public sealed class PaginationValidationFilter : IAsyncActionFilter
{
    public const int MaximumPageSize = 200;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if ((TryGetInt(context, "page", out var page) && page <= 0) ||
            (TryGetInt(context, "pageNumber", out var pageNumber) && pageNumber <= 0))
        {
            context.Result = ValidationProblem("رقم الصفحة يجب أن يكون أكبر من صفر.");
            return;
        }

        if (TryGetInt(context, "pageSize", out var pageSize) &&
            (pageSize <= 0 || pageSize > MaximumPageSize))
        {
            context.Result = ValidationProblem(
                $"حجم الصفحة يجب أن يكون بين 1 و{MaximumPageSize}.");
            return;
        }

        await next();
    }

    private static bool TryGetInt(
        ActionExecutingContext context,
        string key,
        out int value)
    {
        value = 0;
        if (!context.ActionArguments.TryGetValue(key, out var raw) || raw is not int parsed)
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static BadRequestObjectResult ValidationProblem(string message) =>
        new(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "معاملات التصفح غير صالحة.",
            Detail = message
        });
}
