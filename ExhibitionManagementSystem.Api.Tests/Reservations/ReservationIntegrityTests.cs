using System.Security.Claims;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Reservations;

public sealed class ReservationIntegrityTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        _ => { }, NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Create_RejectsBoothFromDifferentVenue()
    {
        await using var context = CreateContext();
        context.Venues.AddRange(
            new Venue { VenueID = 1, TenantID = 1, Name = "Exhibition venue" },
            new Venue { VenueID = 2, TenantID = 1, Name = "Other venue" });
        context.Halls.Add(new Hall { HallID = 20, VenueID = 2, HallName = "Other hall" });
        context.Booths.Add(new Booth
        {
            BoothID = 30,
            TenantID = 1,
            HallID = 20,
            BoothNumber = "B-30",
            OriginalAreaSqM = 12,
            CurrentAreaSqM = 12,
            Status = BoothStatus.Available
        });
        context.Exhibitors.Add(new Exhibitor
        {
            ExhibitorID = 40,
            TenantID = 1,
            CompanyName = "Test exhibitor",
            ExhibitorCategory = ExhibitorCategory.Local
        });
        context.Exhibitions.Add(new Exhibition
        {
            ExhibitionID = 50,
            TenantID = 1,
            VenueID = 1,
            Name = "Test exhibition",
            Status = ExhibitionStatus.Open,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(2),
            EntryCurrency = "LYD"
        });
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new ExhibitionManagementSystem.Services.Implementations.ReservationService(
            unitOfWork, Mapper, null!, null!);

        var result = await service.CreateAsync(1, "admin-1", new BoothReservationCreateDto
        {
            ExhibitorID = 40,
            ExhibitionID = 50,
            BoothID = 30,
            BoothTypeSelected = "Standard",
            RequestedAreaSqM = 12,
            ExhibitorCategory = "Local",
            CurrencyCode = "LYD"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("BOOTH_NOT_IN_EXHIBITION_VENUE", result.ErrorCode);
        Assert.Empty(context.BoothReservations);
    }

    [Fact]
    public async Task Create_UsesTrustedBoothAndExhibitorPricingInputs()
    {
        await using var context = CreateContext();
        context.Tenants.Add(new Tenant { TenantID = 1, CompanyName = "Tenant", BaseCurrency = "LYD" });
        context.Currencies.Add(new Currency { CurrencyCode = "LYD", CurrencyName = "Dinar", Symbol = "LYD" });
        context.Venues.Add(new Venue { VenueID = 1, TenantID = 1, Name = "Venue" });
        context.Halls.Add(new Hall { HallID = 2, VenueID = 1, HallName = "Hall" });
        context.Booths.Add(new Booth
        {
            BoothID = 3, TenantID = 1, HallID = 2, BoothNumber = "VIP-1",
            OriginalAreaSqM = 20, CurrentAreaSqM = 20,
            BoothType = BoothType.VIP, Status = BoothStatus.Available
        });
        context.Exhibitors.Add(new Exhibitor
        {
            ExhibitorID = 4, TenantID = 1, CompanyName = "International exhibitor",
            Sector = "Technology",
            ExhibitorCategory = ExhibitorCategory.International
        });
        context.Exhibitions.Add(new Exhibition
        {
            ExhibitionID = 5, TenantID = 1, VenueID = 1, Name = "Expo",
            Status = ExhibitionStatus.Open, StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1), EntryCurrency = "LYD"
        });
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var pricing = new CapturingPricingService();
        var service = new ExhibitionManagementSystem.Services.Implementations.ReservationService(
            unitOfWork, Mapper, pricing, null!);

        var result = await service.CreateAsync(1, "admin-1", new BoothReservationCreateDto
        {
            ExhibitorID = 4,
            ExhibitionID = 5,
            BoothID = 3,
            BoothTypeSelected = "Standard",
            RequestedAreaSqM = 1,
            ExhibitorCategory = "Local",
            CurrencyCode = "LYD"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("TEST_STOP", result.ErrorCode);
        Assert.Equal(BoothType.VIP, pricing.BoothType);
        Assert.Equal(ExhibitorCategory.International, pricing.Category);
        Assert.Equal(20, pricing.AreaSqM);
        Assert.Equal("Technology", pricing.ProductCategory);
    }

    [Fact]
    public async Task Create_PersistsSelectedServicesBeforeGeneratingInvoice()
    {
        await using var context = CreateContext();
        context.Currencies.Add(new Currency
        {
            CurrencyCode = "LYD",
            CurrencyName = "Libyan dinar",
            Symbol = "د.ل"
        });
        context.Tenants.Add(new Tenant
        {
            TenantID = 1,
            CompanyName = "Tenant",
            BaseCurrency = "LYD"
        });
        context.Venues.Add(new Venue { VenueID = 1, TenantID = 1, Name = "Venue" });
        context.Halls.Add(new Hall { HallID = 2, VenueID = 1, HallName = "Hall" });
        context.Booths.Add(new Booth
        {
            BoothID = 3, TenantID = 1, HallID = 2, BoothNumber = "A-1",
            OriginalAreaSqM = 10, CurrentAreaSqM = 10,
            BoothType = BoothType.Standard, Status = BoothStatus.Available
        });
        context.Exhibitors.Add(new Exhibitor
        {
            ExhibitorID = 4, TenantID = 1, CompanyName = "Exhibitor",
            ExhibitorCategory = ExhibitorCategory.Local
        });
        context.Exhibitions.Add(new Exhibition
        {
            ExhibitionID = 5, TenantID = 1, VenueID = 1, Name = "Expo",
            Status = ExhibitionStatus.Open, StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1), EntryCurrency = "LYD"
        });
        context.Services.Add(new Service
        {
            ServiceID = 6, TenantID = 1, ServiceName = "Electricity",
            Unit = "Point", DefaultPrice = 25, IsActive = true
        });
        await context.SaveChangesAsync();

        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<ReservationMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var pricing = new CapturingPricingService { StopAfterBoothPricing = false };
        var financial = new CapturingFinancialService(context);
        var service = new ExhibitionManagementSystem.Services.Implementations.ReservationService(
            unitOfWork, mapper, pricing, financial);

        var result = await service.CreateAsync(1, "admin-1", new BoothReservationCreateDto
        {
            ExhibitorID = 4,
            ExhibitionID = 5,
            BoothID = 3,
            BoothTypeSelected = "Standard",
            RequestedAreaSqM = 10,
            ExhibitorCategory = "Local",
            CurrencyCode = "LYD",
            Services =
            [
                new ReservationServiceCreateDto
                {
                    ServiceID = 6,
                    Quantity = 2,
                    CurrencyCode = "LYD"
                }
            ]
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var reservation = await context.BoothReservations
            .Include(item => item.ReservationServices)
            .SingleAsync();
        var reservationService = Assert.Single(reservation.ReservationServices);
        Assert.Equal(100, reservation.BoothAmount);
        Assert.Equal(50, reservation.ServicesAmount);
        Assert.Equal(150, reservation.TotalAmount);
        Assert.Equal(2, reservationService.Quantity);
        Assert.Equal(25, reservationService.UnitPrice);
        Assert.Equal(50, reservationService.TotalPrice);
        Assert.Equal(1, financial.ServiceCountWhenInvoiceWasGenerated);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options, new BypassTenantContext());
    }

    private static HttpContextAccessor CreateHttpContextAccessor()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim("TenantId", "1")
        ], "Test");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private sealed class BypassTenantContext : ITenantContext
    {
        public int? TenantId => null;
        public bool CanBypassTenantIsolation => true;
    }

    private sealed class CapturingPricingService : IPricingService
    {
        public bool StopAfterBoothPricing { get; init; } = true;
        public BoothType BoothType { get; private set; }
        public ExhibitorCategory Category { get; private set; }
        public decimal AreaSqM { get; private set; }
        public string? ProductCategory { get; private set; }

        public Task<ServiceResult<BoothPriceCalculationDto>> CalculateBoothPriceDetailsAsync(
            int tenantId, int? exhibitionId, BoothType boothType, ExhibitorCategory category,
            decimal areaSqM, int? boothId = null, string? productCategory = null)
        {
            BoothType = boothType;
            Category = category;
            AreaSqM = areaSqM;
            ProductCategory = productCategory;
            return Task.FromResult(StopAfterBoothPricing
                ? ServiceResult<BoothPriceCalculationDto>.Failure("stop", "TEST_STOP")
                : ServiceResult<BoothPriceCalculationDto>.Success(new BoothPriceCalculationDto
                {
                    Amount = 100,
                    CurrencyCode = "LYD"
                }));
        }

        public Task<ServiceResult<decimal>> CalculateBoothPriceAsync(int tenantId, int? exhibitionId, BoothType boothType, ExhibitorCategory category, decimal areaSqM, int? boothId = null, string? productCategory = null) => throw new NotSupportedException();
        public Task<ServiceResult<decimal>> CalculateServicePriceAsync(int tenantId, int serviceId, int? exhibitionId, int quantity) =>
            Task.FromResult(ServiceResult<decimal>.Success(25m * quantity));
        public Task<ServiceResult<ServicePriceCalculationDto>> CalculateServicePriceDetailsAsync(int tenantId, int serviceId, int? exhibitionId, int quantity, ExhibitorCategory? exhibitorCategory = null) =>
            Task.FromResult(ServiceResult<ServicePriceCalculationDto>.Success(new ServicePriceCalculationDto
            {
                UnitPrice = 25m,
                TotalAmount = 25m * quantity,
                CurrencyCode = "LYD"
            }));
        public Task<ServiceResult<IList<BoothPriceRuleDto>>> GetBoothPriceRulesAsync(int tenantId, int? exhibitionId) => throw new NotSupportedException();
        public Task<ServiceResult<BoothPriceRuleDto>> CreateBoothPriceRuleAsync(int tenantId, BoothPriceRuleCreateDto dto) => throw new NotSupportedException();
        public Task<ServiceResult<BoothPriceRuleDto>> UpdateBoothPriceRuleAsync(int tenantId, int ruleId, BoothPriceRuleCreateDto dto) => throw new NotSupportedException();
        public Task<ServiceResult> DeleteBoothPriceRuleAsync(int tenantId, int ruleId) => throw new NotSupportedException();
        public Task<ServiceResult<IList<ServicePriceRuleDto>>> GetServicePriceRulesAsync(int tenantId, int? exhibitionId) => throw new NotSupportedException();
        public Task<ServiceResult<ServicePriceRuleDto>> CreateServicePriceRuleAsync(int tenantId, ServicePriceRuleCreateDto dto) => throw new NotSupportedException();
        public Task<ServiceResult<IList<PricingPackageDto>>> GetPackagesAsync(int tenantId) => throw new NotSupportedException();
        public Task<ServiceResult<PricingPackageDto>> CreatePackageAsync(int tenantId, PricingPackageCreateDto dto) => throw new NotSupportedException();
    }

    private sealed class CapturingFinancialService(ApplicationDbContext context) : IFinancialService
    {
        public int ServiceCountWhenInvoiceWasGenerated { get; private set; }

        public async Task<ServiceResult<InvoiceDto>> GenerateInvoiceForReservationAsync(int tenantId, int reservationId)
        {
            ServiceCountWhenInvoiceWasGenerated = await context.ReservationServices
                .CountAsync(item => item.ReservationID == reservationId);
            return ServiceResult<InvoiceDto>.Success(new InvoiceDto { ReservationID = reservationId });
        }

        public Task<ServiceResult<PagedResultDto<InvoiceDto>>> GetInvoicesByTenantAsync(int tenantId, int page, int pageSize) => throw new NotSupportedException();
        public Task<ServiceResult<InvoiceDto>> GetInvoiceByIdAsync(int tenantId, int invoiceId) => throw new NotSupportedException();
        public Task<ServiceResult<InvoiceDto>> GetInvoiceByReservationAsync(int tenantId, int reservationId) => throw new NotSupportedException();
        public Task<ServiceResult<IList<InvoiceDto>>> GetOverdueInvoicesAsync(int tenantId) => throw new NotSupportedException();
        public Task<ServiceResult<InvoiceDto>> CreateInvoiceAsync(int tenantId, InvoiceCreateDto dto) => throw new NotSupportedException();
        public Task<ServiceResult<PaymentDto>> RecordPaymentAsync(int tenantId, string userId, PaymentCreateDto dto) => throw new NotSupportedException();
        public Task<ServiceResult<IList<PaymentDto>>> GetPaymentsByInvoiceAsync(int tenantId, int invoiceId) => throw new NotSupportedException();
        public Task<ServiceResult<CreditNoteDto>> CreateCreditNoteAsync(int tenantId, string userId, int invoiceId, CreditNoteCreateDto dto) => throw new NotSupportedException();
        public Task<ServiceResult<IList<CreditNoteDto>>> GetCreditNotesByInvoiceAsync(int tenantId, int invoiceId) => throw new NotSupportedException();
    }
}
