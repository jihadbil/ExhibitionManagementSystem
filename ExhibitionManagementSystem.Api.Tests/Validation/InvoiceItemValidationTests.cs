using System.ComponentModel.DataAnnotations;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Validation;

public sealed class InvoiceItemValidationTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Create_QuantityMustBePositive(string rawQuantity)
    {
        var dto = new InvoiceItemCreateDto
        {
            ItemName = "Booth rental",
            Quantity = decimal.Parse(rawQuantity),
            UnitPrice = 100m
        };

        Assert.Contains(Validate(dto), result =>
            result.MemberNames.Contains(nameof(InvoiceItemCreateDto.Quantity)));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-100")]
    public void Update_UnitPriceCannotBeNegative(string rawUnitPrice)
    {
        var dto = new InvoiceItemUpdateDto
        {
            InvoiceItemID = 1,
            ItemName = "Booth rental",
            Quantity = 1m,
            UnitPrice = decimal.Parse(rawUnitPrice)
        };

        Assert.Contains(Validate(dto), result =>
            result.MemberNames.Contains(nameof(InvoiceItemUpdateDto.UnitPrice)));
    }

    private static List<ValidationResult> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        return results;
    }
}
