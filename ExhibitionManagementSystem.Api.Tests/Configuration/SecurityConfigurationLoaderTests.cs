using ExhibitionManagementSystem.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Configuration;

public sealed class SecurityConfigurationLoaderTests
{
    [Fact]
    public void Load_RejectsMissingConnectionString()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = ValidSecret,
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
            ["JwtSettings:AccessTokenExpiryMinutes"] = "15",
            ["JwtSettings:RefreshTokenExpiryDays"] = "7"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => SecurityConfigurationLoader.Load(configuration));

        Assert.Contains("ConnectionStrings:DefaultConnection", exception.Message);
    }

    [Fact]
    public void Load_RejectsShortJwtKey()
    {
        var values = ValidValues();
        values["JwtSettings:SecretKey"] = "too-short";

        var exception = Assert.Throws<InvalidOperationException>(
            () => SecurityConfigurationLoader.Load(BuildConfiguration(values)));

        Assert.Contains("at least 32 bytes", exception.Message);
    }

    [Fact]
    public void Load_RejectsNonLocalHttpCorsOrigin()
    {
        var values = ValidValues();
        values["Security:AllowedCorsOrigins:0"] = "http://example.com";

        var exception = Assert.Throws<InvalidOperationException>(
            () => SecurityConfigurationLoader.Load(BuildConfiguration(values)));

        Assert.Contains("unsafe origin", exception.Message);
    }

    [Fact]
    public void Load_AcceptsCompleteSecureConfiguration()
    {
        var values = ValidValues();
        values["Security:AllowedCorsOrigins:0"] = "https://portal.example.com";

        var result = SecurityConfigurationLoader.Load(BuildConfiguration(values));

        Assert.Equal("issuer", result.JwtIssuer);
        Assert.Equal(15, result.AccessTokenExpiryMinutes);
        Assert.Single(result.AllowedCorsOrigins);
    }

    private const string ValidSecret =
        "unit-test-secret-with-at-least-thirty-two-bytes";

    private static Dictionary<string, string?> ValidValues() => new()
    {
        ["ConnectionStrings:DefaultConnection"] =
            "Server=(local);Database=Tests;Trusted_Connection=True;TrustServerCertificate=True;",
        ["JwtSettings:SecretKey"] = ValidSecret,
        ["JwtSettings:Issuer"] = "issuer",
        ["JwtSettings:Audience"] = "audience",
        ["JwtSettings:AccessTokenExpiryMinutes"] = "15",
        ["JwtSettings:RefreshTokenExpiryDays"] = "7"
    };

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
