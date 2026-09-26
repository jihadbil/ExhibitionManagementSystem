using System.ComponentModel.DataAnnotations;
using ExhibitionManagementSystem.Controllers.Visitor;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Validation;

public class ReservationAndTicketValidationTests
{
    [Fact]
    public void Reservation_RequiresExactlyOneBoothTarget()
    {
        var neither = ValidReservation();
        neither.BoothID = null;
        neither.MergeID = null;

        var both = ValidReservation();
        both.BoothID = 1;
        both.MergeID = 2;

        Assert.False(IsValid(neither));
        Assert.False(IsValid(both));
    }

    [Fact]
    public void Reservation_RejectsNonPositiveArea()
    {
        var dto = ValidReservation();
        dto.RequestedAreaSqM = 0;

        Assert.False(IsValid(dto));
    }

    [Fact]
    public void Ticket_AllowsFreePriceButRejectsNegativePrice()
    {
        var dto = new TicketCreateDto
        {
            VisitorID = 1,
            ExhibitionID = 1,
            TicketType = "General",
            CurrencyCode = "LYD",
            Price = 0
        };

        Assert.True(IsValid(dto));
        dto.Price = -0.01m;
        Assert.False(IsValid(dto));
    }

    [Theory]
    [InlineData("In", true)]
    [InlineData("out", true)]
    [InlineData("Entry", false)]
    [InlineData("", false)]
    public void TicketScan_AcceptsOnlySupportedDirections(string direction, bool expectedValid)
    {
        var request = new ScanTicketRequest("ticket-qr", direction, "Gate A");

        Assert.Equal(expectedValid, IsValid(request));
    }

    private static BoothReservationCreateDto ValidReservation() => new()
    {
        ExhibitorID = 1,
        ExhibitionID = 1,
        BoothID = 1,
        BoothTypeSelected = "Standard",
        RequestedAreaSqM = 10,
        ExhibitorCategory = "Local",
        CurrencyCode = "LYD"
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
