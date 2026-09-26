using System.Text;

namespace ExhibitionManagementSystem.Configuration;

public sealed record SecurityConfiguration(
    string ConnectionString,
    string JwtSecretKey,
    string JwtIssuer,
    string JwtAudience,
    int AccessTokenExpiryMinutes,
    int RefreshTokenExpiryDays,
    string[] AllowedCorsOrigins);

public static class SecurityConfigurationLoader
{
    private const int MinimumJwtKeyBytes = 32;

    public static SecurityConfiguration Load(IConfiguration configuration)
    {
        var connectionString = Require(
            configuration.GetConnectionString("DefaultConnection"),
            "ConnectionStrings:DefaultConnection");

        var jwtSecretKey = Require(
            configuration["JwtSettings:SecretKey"],
            "JwtSettings:SecretKey");

        if (Encoding.UTF8.GetByteCount(jwtSecretKey) < MinimumJwtKeyBytes)
        {
            throw new InvalidOperationException(
                $"JwtSettings:SecretKey must be at least {MinimumJwtKeyBytes} bytes.");
        }

        var jwtIssuer = Require(configuration["JwtSettings:Issuer"], "JwtSettings:Issuer");
        var jwtAudience = Require(configuration["JwtSettings:Audience"], "JwtSettings:Audience");

        var accessTokenExpiryMinutes = RequirePositiveInt(
            configuration["JwtSettings:AccessTokenExpiryMinutes"],
            "JwtSettings:AccessTokenExpiryMinutes");

        var refreshTokenExpiryDays = RequirePositiveInt(
            configuration["JwtSettings:RefreshTokenExpiryDays"],
            "JwtSettings:RefreshTokenExpiryDays");

        var allowedCorsOrigins = configuration
            .GetSection("Security:AllowedCorsOrigins")
            .Get<string[]>() ?? [];

        foreach (var origin in allowedCorsOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback))
            {
                throw new InvalidOperationException(
                    $"Security:AllowedCorsOrigins contains an unsafe origin: {origin}");
            }
        }

        return new SecurityConfiguration(
            connectionString,
            jwtSecretKey,
            jwtIssuer,
            jwtAudience,
            accessTokenExpiryMinutes,
            refreshTokenExpiryDays,
            allowedCorsOrigins);
    }

    private static string Require(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required security configuration '{key}' is missing.");
        }

        return value;
    }

    private static int RequirePositiveInt(string? value, string key)
    {
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            throw new InvalidOperationException(
                $"Required security configuration '{key}' must be a positive integer.");
        }

        return parsed;
    }
}
