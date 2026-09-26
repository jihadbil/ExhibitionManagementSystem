using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Middleware;

public sealed class IdempotencyMiddleware
{
    public const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan RecordLifetime = TimeSpan.FromHours(24);
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public IdempotencyMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsProtectedRequest(context.Request))
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers[HeaderName].ToString().Trim();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "مفتاح Idempotency مطلوب",
                $"أرسل الرأس {HeaderName} بقيمة لا تتجاوز 100 محرف.");
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantValue = context.User.FindFirstValue("TenantId");
        if (string.IsNullOrWhiteSpace(userId) ||
            !int.TryParse(tenantValue, out var tenantId) ||
            tenantId <= 0)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "هوية غير صالحة",
                "تعذر تحديد المستخدم أو المستأجر من رمز الوصول.");
            return;
        }

        var requestHash = await ComputeRequestHashAsync(context.Request);
        var operation = $"{context.Request.Method}:{context.Request.Path.Value?.ToLowerInvariant()}";
        var decision = await BeginAsync(tenantId, userId, operation, key, requestHash);

        if (decision.Kind == IdempotencyDecisionKind.Replay)
        {
            context.Response.StatusCode = decision.StatusCode!.Value;
            context.Response.ContentType = decision.ContentType ?? "application/json; charset=utf-8";
            await context.Response.WriteAsync(decision.ResponseBody ?? string.Empty);
            return;
        }

        if (decision.Kind == IdempotencyDecisionKind.Conflict)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "تعارض مفتاح Idempotency",
                "استُخدم المفتاح نفسه سابقًا لطلب مختلف.");
            return;
        }

        if (decision.Kind == IdempotencyDecisionKind.InProgress)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "الطلب قيد التنفيذ",
                "يوجد طلب آخر بالمفتاح نفسه قيد التنفيذ؛ أعد المحاولة لاحقًا.");
            return;
        }

        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);

            responseBuffer.Position = 0;
            var responseBody = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true)
                .ReadToEndAsync();

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                await CompleteAsync(
                    tenantId,
                    operation,
                    key,
                    context.Response.StatusCode,
                    context.Response.ContentType,
                    responseBody);
            }
            else
            {
                await AbandonAsync(tenantId, operation, key);
            }

            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalBody);
        }
        catch
        {
            await AbandonAsync(tenantId, operation, key);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    internal static bool IsProtectedRequest(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        return request.Path.Equals("/api/reservations", StringComparison.OrdinalIgnoreCase) ||
               request.Path.Equals("/api/financial/payments", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IdempotencyDecision> BeginAsync(
        int tenantId,
        string userId,
        string operation,
        string key,
        string requestHash)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var existing = await dbContext.IdempotencyRecords
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(record =>
                record.TenantID == tenantId &&
                record.Operation == operation &&
                record.Key == key);

        if (existing != null && existing.ExpiresAt <= now)
        {
            dbContext.IdempotencyRecords.Remove(existing);
            await dbContext.SaveChangesAsync();
            existing = null;
        }

        if (existing != null)
        {
            return ToDecision(existing, userId, requestHash);
        }

        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            TenantID = tenantId,
            UserId = userId,
            Operation = operation,
            Key = key,
            RequestHash = requestHash,
            CreatedAt = now,
            ExpiresAt = now.Add(RecordLifetime)
        });

        try
        {
            await dbContext.SaveChangesAsync();
            return IdempotencyDecision.Started();
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var winner = await dbContext.IdempotencyRecords
                .IgnoreQueryFilters()
                .SingleAsync(record =>
                    record.TenantID == tenantId &&
                    record.Operation == operation &&
                    record.Key == key);
            return ToDecision(winner, userId, requestHash);
        }
    }

    private async Task CompleteAsync(
        int tenantId,
        string operation,
        string key,
        int statusCode,
        string? contentType,
        string responseBody)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var record = await dbContext.IdempotencyRecords
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(candidate =>
                candidate.TenantID == tenantId &&
                candidate.Operation == operation &&
                candidate.Key == key);

        if (record == null)
        {
            return;
        }

        record.State = "Completed";
        record.StatusCode = statusCode;
        record.ContentType = contentType;
        record.ResponseBody = responseBody;
        record.CompletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private async Task AbandonAsync(int tenantId, string operation, string key)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var record = await dbContext.IdempotencyRecords
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(candidate =>
                candidate.TenantID == tenantId &&
                candidate.Operation == operation &&
                candidate.Key == key &&
                candidate.State == "Pending");

        if (record != null)
        {
            dbContext.IdempotencyRecords.Remove(record);
            await dbContext.SaveChangesAsync();
        }
    }

    private static IdempotencyDecision ToDecision(
        IdempotencyRecord record,
        string userId,
        string requestHash)
    {
        if (!string.Equals(record.UserId, userId, StringComparison.Ordinal) ||
            !string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return IdempotencyDecision.Conflict();
        }

        return string.Equals(record.State, "Completed", StringComparison.Ordinal)
            ? IdempotencyDecision.Replay(
                record.StatusCode ?? StatusCodes.Status200OK,
                record.ContentType,
                record.ResponseBody)
            : IdempotencyDecision.InProgress();
    }

    private static async Task<string> ComputeRequestHashAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var memory = new MemoryStream();
        await request.Body.CopyToAsync(memory);
        request.Body.Position = 0;
        return Convert.ToHexString(SHA256.HashData(memory.ToArray()));
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    private enum IdempotencyDecisionKind
    {
        Started,
        Replay,
        Conflict,
        InProgress
    }

    private sealed record IdempotencyDecision(
        IdempotencyDecisionKind Kind,
        int? StatusCode = null,
        string? ContentType = null,
        string? ResponseBody = null)
    {
        public static IdempotencyDecision Started() => new(IdempotencyDecisionKind.Started);
        public static IdempotencyDecision Replay(int statusCode, string? contentType, string? responseBody) =>
            new(IdempotencyDecisionKind.Replay, statusCode, contentType, responseBody);
        public static IdempotencyDecision Conflict() => new(IdempotencyDecisionKind.Conflict);
        public static IdempotencyDecision InProgress() => new(IdempotencyDecisionKind.InProgress);
    }
}
