using System.ComponentModel.DataAnnotations;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class CompanyRegistrationDtoTests
{
    [Fact]
    public void ValidCompanyRegistration_PassesModelValidation()
    {
        var dto = ValidRequest();

        var errors = Validate(dto);

        Assert.Empty(errors);
    }

    [Fact]
    public void Registration_RequiresTermsAndMatchingPasswords()
    {
        var dto = ValidRequest();
        dto.AcceptTerms = false;
        dto.ConfirmPassword = "DifferentPassword123!";

        var errors = Validate(dto);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(dto.AcceptTerms)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(dto.ConfirmPassword)));
    }

    [Theory]
    [InlineData("UpperCase")]
    [InlineData("invalid_domain")]
    [InlineData("-starts-with-dash")]
    [InlineData("ends-with-dash-")]
    public void Registration_RejectsInvalidSubdomains(string subdomain)
    {
        var dto = ValidRequest();
        dto.Subdomain = subdomain;

        var errors = Validate(dto);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(dto.Subdomain)));
    }

    private static CompanyRegistrationRequestDto ValidRequest() => new()
    {
        CompanyName = "شركة الريادة لتنظيم المعارض",
        Subdomain = "alriyada-expo",
        AdminFullName = "مدير الشركة",
        AdminEmail = "admin@alriyada.test",
        AdminPhoneNumber = "+218910000000",
        Password = "StrongPassword123!",
        ConfirmPassword = "StrongPassword123!",
        BaseCurrency = "LYD",
        AcceptTerms = true
    };

    private static List<ValidationResult> Validate(CompanyRegistrationRequestDto dto)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), errors, validateAllProperties: true);
        return errors;
    }
}
