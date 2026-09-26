using ExhibitionManagementSystem.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Validation;

public sealed class PaginationValidationFilterTests
{
    [Theory]
    [InlineData("page", 0)]
    [InlineData("pageNumber", -1)]
    [InlineData("pageSize", 0)]
    [InlineData("pageSize", PaginationValidationFilter.MaximumPageSize + 1)]
    public async Task InvalidPagination_IsRejected(string argumentName, int value)
    {
        var nextCalled = false;
        var context = CreateContext(new Dictionary<string, object?>
        {
            [argumentName] = value
        });

        await new PaginationValidationFilter().OnActionExecutionAsync(
            context,
            () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

        Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task ValidPagination_ContinuesPipeline()
    {
        var nextCalled = false;
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["page"] = 1,
            ["pageSize"] = PaginationValidationFilter.MaximumPageSize
        });

        await new PaginationValidationFilter().OnActionExecutionAsync(
            context,
            () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateContext(
        IDictionary<string, object?> actionArguments)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments,
            new object());
    }

    private static ActionExecutedContext CreateExecutedContext(
        ActionExecutingContext context) =>
        new(context, context.Filters, context.Controller);
}
