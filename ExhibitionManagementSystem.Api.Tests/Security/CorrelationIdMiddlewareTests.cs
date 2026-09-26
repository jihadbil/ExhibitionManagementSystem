using ExhibitionManagementSystem.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvalidCallerValue_IsReplacedWithServerGeneratedIdentifier()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "untrusted-value";
        string? observedIdentifier = null;
        var middleware = new CorrelationIdMiddleware(nextContext =>
        {
            observedIdentifier = nextContext.TraceIdentifier;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            context,
            NullLogger<CorrelationIdMiddleware>.Instance);

        Assert.NotEqual("untrusted-value", observedIdentifier);
        Assert.True(Guid.TryParseExact(observedIdentifier, "N", out _));
    }

    [Fact]
    public async Task ValidGuid_IsNormalizedAndUsedAsTraceIdentifier()
    {
        var identifier = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = identifier.ToString();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            NullLogger<CorrelationIdMiddleware>.Instance);

        Assert.Equal(identifier.ToString("N"), context.TraceIdentifier);
    }
}
