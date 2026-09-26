using ExhibitionManagementSystem.Services.Security;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class RefreshTokenProtectorTests
{
    [Fact]
    public void Hash_DoesNotStoreRawToken_AndVerifyAcceptsOriginal()
    {
        const string token = "a-sensitive-refresh-token";

        var hash = RefreshTokenProtector.Hash(token);

        Assert.NotEqual(token, hash);
        Assert.True(RefreshTokenProtector.Verify(token, hash));
    }

    [Fact]
    public void Verify_RejectsWrongOrLegacyPlaintextValue()
    {
        var hash = RefreshTokenProtector.Hash("correct-token");

        Assert.False(RefreshTokenProtector.Verify("wrong-token", hash));
        Assert.False(RefreshTokenProtector.Verify("correct-token", "legacy-plaintext"));
    }
}
