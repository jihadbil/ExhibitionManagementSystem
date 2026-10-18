using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Visitors;

public sealed class VisitorServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<VisitorMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void VisitorCreateDto_AllowsMissingEmail()
    {
        var dto = CreateVisitor("Visitor one", null);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            validationResults,
            validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public async Task Register_AllowsMultipleVisitorsWithoutEmailAndListsThem()
    {
        await using var context = CreateContext();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new VisitorService(unitOfWork, Mapper);

        var first = await service.RegisterAsync(1, CreateVisitor("Visitor one", null));
        var second = await service.RegisterAsync(1, CreateVisitor("Visitor two", "   "));
        var listed = await service.GetByTenantAsync(1, 1, 20);

        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.True(listed.IsSuccess, listed.ErrorMessage);
        Assert.Equal(2, listed.Data!.TotalCount);
        Assert.All(listed.Data.Items, visitor => Assert.Equal(string.Empty, visitor.Email));
    }

    [Fact]
    public async Task Register_StillRejectsDuplicateProvidedEmail()
    {
        await using var context = CreateContext();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new VisitorService(unitOfWork, Mapper);

        var first = await service.RegisterAsync(1, CreateVisitor("Visitor one", "one@example.com"));
        var duplicate = await service.RegisterAsync(1, CreateVisitor("Visitor two", "one@example.com"));

        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal("EMAIL_ALREADY_EXISTS", duplicate.ErrorCode);
    }

    [Fact]
    public async Task Update_ChangesEditableFieldsAndPreservesRegistrationData()
    {
        await using var context = CreateContext();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new VisitorService(unitOfWork, Mapper);
        var registered = await service.RegisterAsync(1, CreateVisitor("Visitor one", "old@example.com"));
        var registeredAt = registered.Data!.RegisteredAt;

        var updated = await service.UpdateAsync(1, registered.Data.VisitorID, new VisitorUpdateDto
        {
            FullName = "  Visitor updated  ",
            Email = " new@example.com ",
            Phone = " 0910000000 ",
            Nationality = " ليبي ",
            VisitorType = "VIP"
        });

        Assert.True(updated.IsSuccess, updated.ErrorMessage);
        Assert.Equal("Visitor updated", updated.Data!.FullName);
        Assert.Equal("new@example.com", updated.Data.Email);
        Assert.Equal("0910000000", updated.Data.Phone);
        Assert.Equal("ليبي", updated.Data.Nationality);
        Assert.Equal("VIP", updated.Data.VisitorType);
        Assert.Equal(registeredAt, updated.Data.RegisteredAt);
        Assert.NotNull(updated.Data.UpdatedAt);
    }

    [Fact]
    public async Task Update_RejectsAnotherVisitorsEmailAndCrossTenantAccess()
    {
        await using var context = CreateContext();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new VisitorService(unitOfWork, Mapper);
        var first = await service.RegisterAsync(1, CreateVisitor("Visitor one", "one@example.com"));
        var second = await service.RegisterAsync(1, CreateVisitor("Visitor two", "two@example.com"));
        var dto = new VisitorUpdateDto
        {
            FullName = "Visitor two",
            Email = "one@example.com",
            VisitorType = "Regular"
        };

        var duplicate = await service.UpdateAsync(1, second.Data!.VisitorID, dto);
        var crossTenant = await service.UpdateAsync(2, first.Data!.VisitorID, dto);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("EMAIL_ALREADY_EXISTS", duplicate.ErrorCode);
        Assert.False(crossTenant.IsSuccess);
        Assert.Equal("VISITOR_NOT_FOUND", crossTenant.ErrorCode);
    }

    private static VisitorCreateDto CreateVisitor(string name, string? email) => new()
    {
        FullName = name,
        Email = email,
        Phone = string.Empty,
        Nationality = "ليبي",
        VisitorType = "Regular"
    };

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
}
