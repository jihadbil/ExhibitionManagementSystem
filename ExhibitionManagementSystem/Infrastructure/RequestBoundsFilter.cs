using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class RequestBoundsFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var (name, values) in context.HttpContext.Request.Query)
        {
            var max = name.ToLowerInvariant() switch
            {
                "pagesize" => 200,
                "limit" or "count" => 100,
                "page" or "pagenumber" => int.MaxValue,
                _ => 0
            };
            if (max != 0 && (!int.TryParse(values.ToString(), out var value) || value < 1 || value > max))
            {
                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Invalid query bounds", Status = 400,
                    Detail = $"{name} must be between 1 and {max}."
                });
                return;
            }
        }
        var query = context.HttpContext.Request.Query;
        var pageText = query["page"].FirstOrDefault() ?? query["pageNumber"].FirstOrDefault() ?? "1";
        var sizeText = query["pageSize"].FirstOrDefault() ?? "200";
        if (int.TryParse(pageText, out var page) && int.TryParse(sizeText, out var size)
            && (long)(page - 1) * size > int.MaxValue)
            context.Result = new BadRequestObjectResult(new ProblemDetails { Status = 400, Title = "Page offset is too large." });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
