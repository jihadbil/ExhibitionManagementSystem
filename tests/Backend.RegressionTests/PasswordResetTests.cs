using System.Net;
using System.Net.Http.Json;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Backend.RegressionTests;

public sealed partial class IntegrityTests
{
    private WebApplicationFactory<Program> MailFactory(FakeResetTransport sender, TestClock clock) =>
        fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPasswordResetEmailTransport>();
            services.AddSingleton<IPasswordResetEmailTransport>(sender);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
            var worker = services.Single(d => d.ServiceType == typeof(IHostedService)
                && d.ImplementationType == typeof(PasswordResetEmailWorker));
            services.Remove(worker);
            services.Configure<PasswordResetOptions>(options =>
            {
                options.Enabled = true; options.SmtpHost = "smtp.test.invalid";
                options.SenderAddress = "sender@test.invalid"; options.ResetUrl = "https://test.invalid/reset";
            });
        }));

    [Fact]
    public async Task Disabled_reset_delivery_returns_503_for_both_known_and_unknown_accounts()
    {
        using var s = await fixture.CreateScenarioAsync();
        using var client = fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        foreach (var email in new[] { s.Admin.Email, Guid.NewGuid() + "@test.invalid" })
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/Auth/forgot-password", new { email })).StatusCode);
        Assert.False(await s.Db.PasswordResetMessages.AnyAsync(m => m.UserId == s.Admin.Id));
    }

    [Fact]
    public async Task Reset_messages_are_encrypted_retried_delivered_and_consumed_once()
    {
        using var s = await fixture.CreateScenarioAsync();
        var sender = new FakeResetTransport { FailuresRemaining = 1 }; var clock = new TestClock();
        await using var factory = MailFactory(sender, clock);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var forgot = await client.PostAsJsonAsync("/api/Auth/forgot-password", new { email = s.Admin.Email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        Assert.DoesNotContain("token", await forgot.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var queued = await s.Db.PasswordResetMessages.AsNoTracking().SingleAsync(m => m.UserId == s.Admin.Id);
        Assert.DoesNotContain(s.Admin.Email!, queued.ProtectedPayload);
        Assert.NotEmpty(queued.ProtectedPayload);
        // Immediate repeated requests use the pending message rather than creating duplicates.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Auth/forgot-password", new { email = s.Admin.Email })).StatusCode);
        Assert.Equal(1, await s.Db.PasswordResetMessages.CountAsync(m => m.UserId == s.Admin.Id));
        async Task Dispatch()
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<PasswordResetDispatcher>().ProcessBatchAsync();
        }
        await Dispatch();
        var failed = await s.Db.PasswordResetMessages.AsNoTracking().SingleAsync(m => m.Id == queued.Id);
        Assert.Null(failed.CompletedAt); Assert.Equal("DELIVERY_FAILED", failed.LastErrorCode);
        Assert.NotEmpty(failed.ProtectedPayload); Assert.Empty(sender.Delivered);
        clock.Advance(TimeSpan.FromMinutes(2)); await Dispatch();
        var sent = await s.Db.PasswordResetMessages.AsNoTracking().SingleAsync(m => m.Id == queued.Id);
        Assert.NotNull(sent.SentAt); Assert.NotNull(sent.CompletedAt); Assert.Empty(sent.ProtectedPayload);
        var delivered = Assert.Single(sender.Delivered);
        Assert.Equal(s.Admin.Email, delivered.Email);
        var query = QueryHelpers.ParseQuery(delivered.Link.Query);
        Assert.Equal(s.Admin.Email, query["email"].ToString());
        var token = query["token"].ToString(); Assert.NotEmpty(token);
        var password = fixture.Password + "newA1!";
        var request = new { email = s.Admin.Email, token, newPassword = password, confirmPassword = password };
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/Auth/reset-password/confirm", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Auth/reset-password/confirm", request)).StatusCode);
        using var verify = factory.Services.CreateScope();
        var manager = verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await manager.FindByIdAsync(s.Admin.Id))!;
        Assert.True(await manager.CheckPasswordAsync(user, password));
        Assert.False(await manager.CheckPasswordAsync(user, fixture.Password));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Expired_or_revoked_reset_messages_are_not_delivered(bool expire)
    {
        using var s = await fixture.CreateScenarioAsync();
        var sender = new FakeResetTransport(); var clock = new TestClock();
        await using var factory = MailFactory(sender, clock);
        using var scope = factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        Assert.True((await auth.ForgotPasswordAsync(new() { Email = s.Admin.Email! })).IsSuccess);
        Assert.True((await auth.ForgotPasswordAsync(new() { Email = Guid.NewGuid() + "@test.invalid" })).IsSuccess);
        if (expire) clock.Advance(TimeSpan.FromMinutes(31));
        else
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await manager.UpdateSecurityStampAsync((await manager.FindByIdAsync(s.Admin.Id))!)).Succeeded);
        }
        await scope.ServiceProvider.GetRequiredService<PasswordResetDispatcher>().ProcessBatchAsync();
        var message = await s.Db.PasswordResetMessages.AsNoTracking().SingleAsync(m => m.UserId == s.Admin.Id);
        Assert.NotNull(message.CompletedAt); Assert.Empty(message.ProtectedPayload);
        Assert.Equal("EXPIRED_OR_REVOKED", message.LastErrorCode); Assert.Empty(sender.Delivered);
    }

    [Fact]
    public async Task Reset_transport_configuration_requires_https_and_valid_sender()
    {
        var options = new PasswordResetOptions { Enabled = true, SmtpHost = "smtp.test.invalid", SenderAddress = "invalid", ResetUrl = "http://test.invalid" };
        Assert.False(options.IsValid());
        options.SenderAddress = "sender@test.invalid"; options.ResetUrl = "https://test.invalid/reset";
        Assert.True(options.IsValid());
        options.SmtpUser = "user"; Assert.False(options.IsValid());
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan interval) => now += interval;
    }

    private sealed class FakeResetTransport : IPasswordResetEmailTransport
    {
        public int FailuresRemaining { get; set; }
        public List<(string Email, Uri Link)> Delivered { get; } = [];
        public Task SendAsync(string recipient, Uri resetLink, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailuresRemaining-- > 0) throw new IOException("Simulated temporary delivery failure.");
            Delivered.Add((recipient, resetLink)); return Task.CompletedTask;
        }
    }
}
