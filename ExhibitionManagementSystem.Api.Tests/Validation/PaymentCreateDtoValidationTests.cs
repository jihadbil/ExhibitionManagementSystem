using System.ComponentModel.DataAnnotations;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Validation;

public sealed class PaymentCreateDtoValidationTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Amount_MustBePositive(string rawAmount)
    {
        var dto = ValidPayment(decimal.Parse(rawAmount));

        var errors = Validate(dto);

        Assert.Contains(errors, result =>
            result.MemberNames.Contains(nameof(PaymentCreateDto.Amount)));
    }

    [Fact]
    public void PositiveAmount_IsAccepted()
    {
        var errors = Validate(ValidPayment(0.01m));

        Assert.Empty(errors);
    }

    private static PaymentCreateDto ValidPayment(decimal amount) => new()
    {
        InvoiceID = 1,
        Amount = amount,
        CurrencyCode = "LYD",
        Method = "Cash"
    };

    private static List<ValidationResult> Validate(PaymentCreateDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true);
        return results;
    }
}
