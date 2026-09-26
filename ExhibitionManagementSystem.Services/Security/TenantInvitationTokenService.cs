using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace ExhibitionManagementSystem.Services.Security;

public static class TenantInvitationTokenService
{
    public static string GenerateToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Verify(string token, string expectedHash)
    {
        var actualBytes = Encoding.ASCII.GetBytes(Hash(token));
        var expectedBytes = Encoding.ASCII.GetBytes(expectedHash);

        return actualBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
