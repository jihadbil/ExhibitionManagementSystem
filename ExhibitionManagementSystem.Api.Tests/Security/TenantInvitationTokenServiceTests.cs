using ExhibitionManagementSystem.Services.Security;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class TenantInvitationTokenServiceTests
{
    [Fact]
    public void GeneratedToken_IsVerifiedOnlyAgainstItsHash()
    {
        var token = TenantInvitationTokenService.GenerateToken();
        var hash = TenantInvitationTokenService.Hash(token);

        Assert.NotEqual(token, hash);
        Assert.True(TenantInvitationTokenService.Verify(token, hash));
        Assert.False(TenantInvitationTokenService.Verify(token + "changed", hash));
    }

    [Fact]
    public void GeneratedTokens_AreUnique()
    {
        var tokens = Enumerable.Range(0, 100)
            .Select(_ => TenantInvitationTokenService.GenerateToken())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(100, tokens.Count);
    }
}
