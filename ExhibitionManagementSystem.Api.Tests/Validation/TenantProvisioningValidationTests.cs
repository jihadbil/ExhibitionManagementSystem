using System.ComponentModel.DataAnnotations;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Validation;

public sealed class TenantProvisioningValidationTests
{
    [Fact]
    public void TenantCreation_RequiresFirstAdministratorIdentity()
    {
        var dto = ValidTenant();
        dto.AdminEmail = string.Empty;
        dto.AdminFullName = string.Empty;

        Assert.False(IsValid(dto));
    }

    [Theory]
    [InlineData("valid-tenant", true)]
    [InlineData("Valid-Tenant", false)]
    [InlineData("invalid tenant", false)]
    [InlineData("-invalid", false)]
    public void TenantCreation_ValidatesSubdomain(string subdomain, bool expectedValid)
    {
        var dto = ValidTenant();
        dto.Subdomain = subdomain;

        Assert.Equal(expectedValid, IsValid(dto));
    }

    [Fact]
    public void TenantCreation_RejectsExpiredTrial()
    {
        var dto = ValidTenant();
        dto.TrialEndsAt = DateTime.UtcNow.AddDays(-1);

        Assert.False(IsValid(dto));
    }

    [Fact]
    public void InvitationAcceptance_RequiresMatchingPasswordConfirmation()
    {
        var dto = new TenantInvitationAcceptDto
        {
            Email = "admin@example.com",
            Token = new string('a', 43),
            Password = "StrongPass1!",
            ConfirmPassword = "DifferentPass1!"
        };

        Assert.False(IsValid(dto));
    }

    private static TenantCreateDto ValidTenant() => new()
    {
        CompanyName = "Example Exhibitions",
        Subdomain = "example-exhibitions",
        Plan = "Basic",
        BaseCurrency = "LYD",
        AdminEmail = "admin@example.com",
        AdminFullName = "Tenant Administrator",
        TrialEndsAt = DateTime.UtcNow.AddDays(14)
    };

    private static bool IsValid(object dto)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true);
    }
}
