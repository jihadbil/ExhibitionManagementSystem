using System.Security.Claims;
using System.Text;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task ProtectedRequest_WithoutKey_IsRejected()
    {
        using var provider = CreateProvider();
        var middleware = new IdempotencyMiddleware(
            _ => Task.CompletedTask,
            provider.GetRequiredService<IServiceScopeFactory>());
        var context = CreateRequest();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task CompletedRequest_IsReplayedWithoutExecutingHandlerAgain()
    {
        using var provider = CreateProvider();
        var executionCount = 0;
        var middleware = new IdempotencyMiddleware(
            async context =>
            {
                executionCount++;
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"id\":42}");
            },
            provider.GetRequiredService<IServiceScopeFactory>());

        var first = CreateRequest("same-key");
        await middleware.InvokeAsync(first);
        var second = CreateRequest("same-key");
        await middleware.InvokeAsync(second);

        Assert.Equal(1, executionCount);
        Assert.Equal(StatusCodes.Status201Created, second.Response.StatusCode);
        second.Response.Body.Position = 0;
        Assert.Equal("{\"id\":42}", await new StreamReader(second.Response.Body).ReadToEndAsync());
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString("N");
        services.AddScoped<ITenantContext>(_ => new StubTenantContext(1));
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateRequest(string? key = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/reservations";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"boothId\":7}"));
        context.Response.Body = new MemoryStream();
        if (key != null)
        {
            context.Request.Headers[IdempotencyMiddleware.HeaderName] = key;
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim("TenantId", "1")
            ],
            "Test"));
        return context;
    }

    private sealed record StubTenantContext(
        int? TenantId,
        bool CanBypassTenantIsolation = false) : ITenantContext;
}
