using System.Net;
using System.Net.Http.Json;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.RegressionTests;

public sealed partial class IntegrityTests
{
    [Fact]
    public async Task Fractional_invoice_lines_round_individually_so_persisted_totals_match_their_sum()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var invoice = await s.InvoiceAsync();
        var items = s.Services.GetRequiredService<IInvoiceItemService>();
        for (var i = 0; i < 2; i++)
        {
            var added = await items.CreateItemAsync(s.Tenant.TenantID, invoice.InvoiceID,
                new() { ItemName = "Fractional", Quantity = 0.5m, UnitPrice = 0.01m });
            Assert.True(added.IsSuccess, added.ErrorMessage);
            Assert.Equal(0.01m, added.Data!.TotalPrice);
        }
        using var scope = fixture.Factory.Services.CreateScope();
        var saved = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Invoices.AsNoTracking()
            .Include(i => i.InvoiceItems).SingleAsync(i => i.InvoiceID == invoice.InvoiceID);
        Assert.Equal(100.02m, saved.TotalAmount);
        Assert.Equal(saved.SubTotal, saved.InvoiceItems.Sum(i => i.TotalPrice));
    }

    [Theory]
    [InlineData(40)]
    [InlineData(100)]
    public async Task Payment_replays_return_the_original_record_even_after_full_settlement(int amount)
    {
        using var s = await fixture.CreateScenarioAsync();
        var invoice = await s.InvoiceAsync();
        using var client = await ClientAsync(s);
        var request = new PaymentCreateDto { IdempotencyKey = Guid.NewGuid().ToString("N"), InvoiceID = invoice.InvoiceID,
            Amount = amount, CurrencyCode = "USD", Method = "Cash", ReferenceNo = "Receipt 1" };
        using var first = await client.PostAsJsonAsync("/api/Financial/payments", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var original = await first.Content.ReadFromJsonAsync<PaymentDto>();
        using var replay = await client.PostAsJsonAsync("/api/Financial/payments", request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(original!.PaymentID, (await replay.Content.ReadFromJsonAsync<PaymentDto>())!.PaymentID);
        request.ReferenceNo = "Receipt 2";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/Financial/payments", request)).StatusCode);
        var saved = await s.Db.Payments.AsNoTracking().Where(p => p.InvoiceID == invoice.InvoiceID).ToListAsync();
        Assert.Equal(amount, Assert.Single(saved).Amount);
    }

    [Fact]
    public async Task Concurrent_replays_cannot_create_duplicate_payments()
    {
        using var s = await fixture.CreateScenarioAsync();
        var invoice = await s.InvoiceAsync();
        using var client = await ClientAsync(s);
        var request = new PaymentCreateDto { IdempotencyKey = Guid.NewGuid().ToString("N"), InvoiceID = invoice.InvoiceID,
            Amount = 40, CurrencyCode = "USD", Method = "Cash" };
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/Financial/payments", request)));
        Assert.Contains(responses, r => r.IsSuccessStatusCode);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.Conflict));
        using var retry = await client.PostAsJsonAsync("/api/Financial/payments", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Single(await s.Db.Payments.AsNoTracking().Where(p => p.InvoiceID == invoice.InvoiceID).ToListAsync());
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task Payment_keys_are_required_and_do_not_bypass_tenant_ownership()
    {
        using var s = await fixture.CreateScenarioAsync();
        var invoice = await s.InvoiceAsync();
        using var client = await ClientAsync(s);
        var request = new PaymentCreateDto { InvoiceID = invoice.InvoiceID, Amount = 40, CurrencyCode = "USD", Method = "Cash" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Financial/payments", request)).StatusCode);
        request.IdempotencyKey = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Financial/payments", request)).StatusCode);
        using var scope = fixture.Factory.Services.CreateScope();
        Assert.False((await scope.ServiceProvider.GetRequiredService<IFinancialService>()
            .RecordPaymentAsync(s.OtherTenant.TenantID, s.OtherUser.Id, request)).IsSuccess);
    }

    [Fact]
    public async Task Invoice_numbers_are_unique_under_parallel_allocation()
    {
        using var s = await fixture.CreateScenarioAsync();
        var numbers = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            using var scope = fixture.Factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Invoices.GenerateNextInvoiceNumberAsync(s.Tenant.TenantID);
        }));
        Assert.Equal(20, numbers.Distinct().Count());
        Assert.All(numbers, n => Assert.StartsWith($"INV-{s.Tenant.TenantID:D4}-", n));
    }

    private async Task<(Sponsor Sponsor, SponsorshipPackageDto Package, AdvertisingSpaceDto Space)> SponsorshipStockAsync(Scenario s, int capacity = 1)
    {
        var sponsor = BackendFixture.Fill(new Sponsor { TenantID = s.Tenant.TenantID, CompanyName = "Sponsor" });
        s.Db.Add(sponsor); await s.Db.SaveChangesAsync();
        var service = s.Services.GetRequiredService<ISponsorshipService>();
        var package = await service.CreatePackageAsync(s.Tenant.TenantID, new() { ExhibitionID = s.Exhibition.ExhibitionID,
            PackageName = "Gold", CurrencyCode = "USD", MaxSponsorsCount = capacity, Price = 100 });
        Assert.True(package.IsSuccess, package.ErrorMessage);
        var space = await service.CreateSpaceAsync(s.Tenant.TenantID, new() { ExhibitionID = s.Exhibition.ExhibitionID,
            SpaceCode = "A1", SpaceName = "Screen", CurrencyCode = "USD" });
        Assert.True(space.IsSuccess, space.ErrorMessage);
        return (sponsor, package.Data!, space.Data!);
    }

    [Fact]
    public async Task Concurrent_sponsors_cannot_exceed_package_capacity()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var stock = await SponsorshipStockAsync(s);
        using var client = await ClientAsync(s);
        var request = new SponsorshipContractCreateDto { SponsorID = stock.Sponsor.SponsorID, PackageID = stock.Package.PackageID,
            ExhibitionID = s.Exhibition.ExhibitionID, CurrencyCode = "USD", TotalAmount = 100 };
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/Sponsorship/contracts", request)));
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest));
        Assert.Equal(1, await s.Db.SponsorshipContracts.CountAsync(c => c.PackageID == stock.Package.PackageID));
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task Concurrent_assignments_cannot_share_an_advertising_space()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var stock = await SponsorshipStockAsync(s, 3);
        var service = s.Services.GetRequiredService<ISponsorshipService>();
        var contractIds = new List<int>();
        for (var i = 0; i < 2; i++)
        {
            var contract = await service.CreateContractAsync(s.Tenant.TenantID, s.Admin.Id, new() { SponsorID = stock.Sponsor.SponsorID,
                PackageID = stock.Package.PackageID, ExhibitionID = s.Exhibition.ExhibitionID, CurrencyCode = "USD" });
            Assert.True(contract.IsSuccess, contract.ErrorMessage); contractIds.Add(contract.Data!.ContractID);
        }
        using var client = await ClientAsync(s);
        var responses = await Task.WhenAll(contractIds.Select(id => client.PostAsJsonAsync("/api/Sponsorship/assign-space",
            new SponsorAdAssignmentCreateDto { ContractID = id, SpaceID = stock.Space.SpaceID })));
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest));
        Assert.Equal(1, await s.Db.SponsorAdAssignments.CountAsync(a => a.SpaceID == stock.Space.SpaceID));
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task Cancelling_contract_releases_capacity_and_space_and_keeps_assignment_history()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var stock = await SponsorshipStockAsync(s);
        var service = s.Services.GetRequiredService<ISponsorshipService>();
        var request = new SponsorshipContractCreateDto { SponsorID = stock.Sponsor.SponsorID, PackageID = stock.Package.PackageID,
            ExhibitionID = s.Exhibition.ExhibitionID, CurrencyCode = "USD", SpaceIDsToAssign = [stock.Space.SpaceID] };
        var contract = await service.CreateContractAsync(s.Tenant.TenantID, s.Admin.Id, request);
        Assert.True(contract.IsSuccess, contract.ErrorMessage);
        Assert.False((await service.UpdateSpaceAsync(s.Tenant.TenantID, stock.Space.SpaceID, new() {
            ExhibitionID = s.Exhibition.ExhibitionID, SpaceCode = "A1", SpaceName = "Screen", CurrencyCode = "USD", IsAvailable = true })).IsSuccess);
        Assert.False((await service.DeletePackageAsync(s.Tenant.TenantID, stock.Package.PackageID, s.Admin.Id)).IsSuccess);
        using var client = await ClientAsync(s);
        Assert.True((await client.PutAsJsonAsync($"/api/Sponsorship/contracts/{contract.Data!.ContractID}/status", "Cancelled")).IsSuccessStatusCode);
        Assert.False((await client.PutAsJsonAsync($"/api/Sponsorship/contracts/{contract.Data.ContractID}/status", "Confirmed")).IsSuccessStatusCode);
        Assert.True((await s.Db.AdvertisingSpaces.AsNoTracking().SingleAsync(a => a.SpaceID == stock.Space.SpaceID)).IsAvailable);
        Assert.Empty(await s.Db.SponsorAdAssignments.AsNoTracking().Where(a => a.ContractID == contract.Data.ContractID).ToListAsync());
        Assert.Single(await s.Db.SponsorAdAssignments.IgnoreQueryFilters().AsNoTracking().Where(a => a.ContractID == contract.Data.ContractID).ToListAsync());
        Assert.True((await client.PostAsJsonAsync("/api/Sponsorship/contracts", request)).IsSuccessStatusCode);
        Assert.Equal(1, await s.Db.SponsorAdAssignments.CountAsync(a => a.SpaceID == stock.Space.SpaceID));
    }

    [Fact]
    public async Task Contract_creation_is_atomic_when_one_requested_space_is_unavailable()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var stock = await SponsorshipStockAsync(s, 3);
        var service = s.Services.GetRequiredService<ISponsorshipService>();
        var result = await service.CreateContractAsync(s.Tenant.TenantID, s.Admin.Id, new() {
            SponsorID = stock.Sponsor.SponsorID, ExhibitionID = s.Exhibition.ExhibitionID, CurrencyCode = "USD",
            SpaceIDsToAssign = [stock.Space.SpaceID, int.MaxValue] });
        Assert.False(result.IsSuccess);
        Assert.False(await s.Db.SponsorshipContracts.AnyAsync(c => c.ExhibitionID == s.Exhibition.ExhibitionID));
        Assert.False(await s.Db.SponsorAdAssignments.AnyAsync(a => a.SpaceID == stock.Space.SpaceID));
        Assert.True((await s.Db.AdvertisingSpaces.AsNoTracking().SingleAsync(a => a.SpaceID == stock.Space.SpaceID)).IsAvailable);
    }

    [Fact]
    public async Task Visitors_search_is_bounded_and_projects_ticket_counts()
    {
        using var s = await fixture.CreateScenarioAsync();
        var term = "Search" + Guid.NewGuid().ToString("N");
        s.Db.Visitors.AddRange(Enumerable.Range(0, 110).Select(i => BackendFixture.Fill(new Visitor {
            TenantID = s.Tenant.TenantID, FullName = term + i.ToString("D3") })));
        await s.Db.SaveChangesAsync();
        var service = s.Services.GetRequiredService<IVisitorService>();
        Assert.False((await service.SearchAsync(s.Tenant.TenantID, " ")).IsSuccess);
        var result = await service.SearchAsync(s.Tenant.TenantID, term);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(100, result.Data!.Count);
        Assert.All(result.Data, v => Assert.Equal(0, v.TicketsCount));
    }

    [Fact]
    public async Task Cancelled_request_aborts_queries_and_business_writes()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var context = s.Services.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        context.RequestAborted = cancellation.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => s.Services.GetRequiredService<IVisitorService>().GetByTenantAsync(s.Tenant.TenantID, 1, 20));
        s.Db.Services.Add(BackendFixture.Fill(new Service { TenantID = s.Tenant.TenantID, ServiceName = "Must not save" }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => s.Services.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        context.RequestAborted = CancellationToken.None;
        using var scope = fixture.Factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Services.AnyAsync(x => x.TenantID == s.Tenant.TenantID && x.ServiceName == "Must not save"));
    }

    [Fact]
    public async Task Malformed_dates_enums_quantities_and_page_offsets_return_400_without_writes()
    {
        using var s = await fixture.CreateScenarioAsync();
        using var client = await ClientAsync(s);
        var exhibition = new ExhibitionManagementSystem.Models.DTOs.Exhibition.ExhibitionCreateDto { VenueID = s.Exhibition.VenueID,
            Name = "Invalid", EntryCurrency = "USD", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(-1) };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Exhibitions", exhibition)).StatusCode);
        var booth = new ExhibitionManagementSystem.Models.DTOs.Booth.BoothUpdateDto { BoothNumber = "A1", Status = "999" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/Booths/{s.Booth.BoothID}", booth)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/Visitors?page=2147483647&pageSize=200")).StatusCode);
        var pricing = await s.Services.GetRequiredService<IPricingService>().CalculateServicePriceAsync(s.Tenant.TenantID, s.Service.ServiceID,
            s.Exhibition.ExhibitionID, 1.001m, "USD");
        Assert.False(pricing.IsSuccess);
        Assert.False(await s.Db.Exhibitions.AnyAsync(e => e.TenantID == s.Tenant.TenantID && e.Name == "Invalid"));
    }
}
