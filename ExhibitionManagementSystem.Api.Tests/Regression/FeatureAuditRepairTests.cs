// Regression tests for the feature audit repairs. Disposable SQL databases only.
using System.Net;
using System.Net.Http.Json;
using System.Data.Common;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoMapper;
using ExhibitionManagementSystem.Api.Tests.Regression;
using ExhibitionManagementSystem.Controllers.Visitor;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Mapping;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.DTOs.Service;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Security;
using ExhibitionManagementSystem.Middleware;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class FeatureAuditRepairTests(ITestOutputHelper output)
{
    static readonly IMapper Mapper = new MapperConfiguration(c => c.AddMaps(typeof(MappingProfile).Assembly), NullLoggerFactory.Instance).CreateMapper();
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    void Record(string name, object value) => output.WriteLine(name + ": " + JsonSerializer.Serialize(value, Json));
    sealed class Scope(int tenantId) : ITenantContext { public int? TenantId => tenantId; public bool CanBypassTenantIsolation => false; }

    [SqlServerTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  Loading access  ")]
    public async Task HttpReservationAcceptsOptionalNotes(string? notes)
    {
        await using var host = await Host.Start();
        var response = await host.Client.PostAsJsonAsync("/api/reservations", new {
            exhibitionID=1, exhibitorID=1, boothID=2, boothTypeSelected="Standard", requestedAreaSqM=10,
            exhibitorCategory="Local", currencyCode="LYD", logisticNotes=notes, services=Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body=await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(notes?.Trim()??"",body.GetProperty("logisticNotes").GetString());
    }

    [SqlServerFact]
    public async Task HttpSponsorshipPackageLevelAndContractShape()
    {
        await using var host = await Host.Start();
        var invalid = await host.Client.PostAsJsonAsync("/api/sponsorship/packages", new { exhibitionID=1, packageName="Web package", level="Gold", price=5000, currencyCode="LYD", maxSponsorsCount=3 });
        var valid = await host.Client.PostAsJsonAsync("/api/sponsorship/packages", new { exhibitionID=1, packageName="Web package", level=2, price=5000, currencyCode="LYD", maxSponsorsCount=3 });
        var response = await host.Client.GetAsync("/api/sponsorship/contracts?exhibitionId=1");
        var contracts = await response.Content.ReadFromJsonAsync<JsonElement>();
        Record("sponsorship-contracts", contracts);
        Record("sponsorship-level", new { webStatus=(int)invalid.StatusCode, body=await invalid.Content.ReadAsStringAsync(), numericStatus=(int)valid.StatusCode });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode); Assert.True(valid.IsSuccessStatusCode);
        Assert.True(contracts[0].TryGetProperty("totalAmount", out _)); Assert.False(contracts[0].TryGetProperty("contractAmount", out _));
        Assert.Equal(JsonValueKind.Number, contracts[0].GetProperty("status").ValueKind);
    }

    [SqlServerFact]
    public async Task HttpInvalidExhibitorCategoryAndWhitespaceEventType()
    {
        await using var host = await Host.Start();
        var category = await host.Client.PostAsJsonAsync("/api/exhibitors", new {companyName="Invalid category",email="new@example.invalid",exhibitorCategory="Invalid",preferredCurrencyCode="LYD"});
        var schedule = await host.Client.PostAsJsonAsync("/api/exhibitions/1/schedules", new {eventName="Whitespace",eventType=" ",startDateTime=DateTime.UtcNow.Date.AddHours(10),endDateTime=DateTime.UtcNow.Date.AddHours(11)});
        Record("enum-validation", new {categoryStatus=(int)category.StatusCode,scheduleStatus=(int)schedule.StatusCode});
        Assert.Equal(HttpStatusCode.BadRequest,category.StatusCode);
        Assert.Equal(HttpStatusCode.OK,schedule.StatusCode);
        var created=await schedule.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(JsonValueKind.Null,created.GetProperty("eventType").ValueKind);
        var badType=await host.Client.PostAsJsonAsync("/api/exhibitions/1/schedules",new {eventName="Invalid",eventType="Invalid",startDateTime=DateTime.UtcNow.Date.AddHours(10),endDateTime=DateTime.UtcNow.Date.AddHours(11)});
        Assert.Equal(HttpStatusCode.BadRequest,badType.StatusCode);
    }

    [SqlServerFact]
    public async Task HttpPaymentPathsShareIdempotencyProtectionAndReplay()
    {
        await using var host=await Host.Start(InvoiceStatus.Issued);
        host.Client.DefaultRequestHeaders.Remove("Idempotency-Key");
        var payment=new {invoiceID=1,amount=25,currencyCode="USD",method="Cash"};
        var canonical=await host.Client.PostAsJsonAsync("/api/financial/payments",payment);
        var first=await host.Client.PostAsJsonAsync("/api/financial/payments/",payment);
        var second=await host.Client.PostAsJsonAsync("/api/financial/payments/",payment);
        var payments=await host.Client.GetFromJsonAsync<JsonElement>("/api/financial/payments/invoice/1");
        Assert.Equal(HttpStatusCode.BadRequest,canonical.StatusCode);Assert.Equal(HttpStatusCode.BadRequest,first.StatusCode);Assert.Equal(HttpStatusCode.BadRequest,second.StatusCode);Assert.Equal(0,payments.GetArrayLength());
        host.Client.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString("N"));
        var paid=await host.Client.PostAsJsonAsync("/api/financial/payments",payment);
        var replay=await host.Client.PostAsJsonAsync("/api/financial/payments/",payment);
        Assert.Equal(HttpStatusCode.OK,paid.StatusCode);Assert.Equal(paid.StatusCode,replay.StatusCode);
        Assert.Equal(await paid.Content.ReadAsStringAsync(),await replay.Content.ReadAsStringAsync());
        Assert.Equal(1,(await host.Client.GetFromJsonAsync<JsonElement>("/api/financial/payments/invoice/1")).GetArrayLength());
    }

    [SqlServerFact]
    public async Task HttpGateOperatorCanPreviewButCannotChangeTemplatesOrIssueStaffPermits()
    {
        await using var host=await Host.Start();
        var tickets=await host.Client.GetFromJsonAsync<JsonElement>("/api/tickets/exhibition/1");
        Record("tickets",tickets);
        var admin=await host.Client.GetAsync("/api/badges/render-by-qr/AUDIT-TICKET");
        host.Client.DefaultRequestHeaders.Add("Probe-Role","GateOperator");
        var gateTickets=await host.Client.GetAsync("/api/tickets/exhibition/1");
        var gatePreview=await host.Client.GetAsync("/api/badges/render-by-qr/AUDIT-TICKET");
        Record("gate-preview",new {adminPreview=(int)admin.StatusCode,gateTickets=(int)gateTickets.StatusCode,gatePreview=(int)gatePreview.StatusCode});
        Assert.True(admin.IsSuccessStatusCode);Assert.True(gateTickets.IsSuccessStatusCode);Assert.Equal(HttpStatusCode.OK,gatePreview.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await host.Client.GetAsync("/api/badges/templates")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await host.Client.PostAsJsonAsync("/api/badges/templates",new {templateName="Forbidden"})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await host.Client.PostAsync("/api/badges/staff/1/issue",null)).StatusCode);
    }

    sealed class ParentReadBarrier(string table) : DbCommandInterceptor
    {
        readonly System.Collections.Concurrent.ConcurrentDictionary<Guid,byte> contexts=new();
        readonly TaskCompletionSource ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,CommandExecutedEventData eventData,DbDataReader reader,CancellationToken token=default)
        {
            if(command.CommandText.Contains($"FROM [{table}]",StringComparison.Ordinal) && eventData.Context!=null && contexts.TryAdd(eventData.Context.ContextId.InstanceId,0))
            {if(contexts.Count==2)ready.TrySetResult();await ready.Task.WaitAsync(TimeSpan.FromSeconds(20),token);}
            return reader;
        }
    }
    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SqlVenueDeletionCannotRaceWithChildCreation(bool exhibitionCase)
    {
        var name="EMS_Feature_Race_"+Guid.NewGuid().ToString("N");
        var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION")){InitialCatalog=name,TrustServerCertificate=true};
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var seed=new ApplicationDbContext(options,new WorkflowFixture.BypassContext());
        try
        {
            await seed.Database.MigrateAsync();var fixture=await WorkflowFixture.SeedAsync(seed);
            var target=new Venue {TenantID=fixture.TenantID,Name="Concurrent target"};seed.Venues.Add(target);await seed.SaveChangesAsync();
            var barrier=new ParentReadBarrier("Venues");
            var deleteOptions=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).AddInterceptors(barrier).Options;
            async Task<bool> Delete()
            {
                await using var db=new ApplicationDbContext(deleteOptions,new Scope(fixture.TenantID));using var uow=new UnitOfWork(db,WorkflowFixture.Actor(fixture.TenantID));
                return (await new VenueService(uow,Mapper).DeleteAsync(fixture.TenantID,target.VenueID)).IsSuccess;
            }
            async Task<bool> Create()
            {
                await using var db=new ApplicationDbContext(deleteOptions,new Scope(fixture.TenantID));using var uow=new UnitOfWork(db,WorkflowFixture.Actor(fixture.TenantID));
                return exhibitionCase
                    ?(await new ExhibitionService(uow,Mapper).CreateAsync(fixture.TenantID,new ExhibitionManagementSystem.Models.DTOs.Exhibition.ExhibitionCreateDto {VenueID=target.VenueID,Name="Concurrent expo",EntryCurrency="LYD",StartDate=DateTime.UtcNow.Date,EndDate=DateTime.UtcNow.Date.AddDays(1)})).IsSuccess
                    :(await new HallService(uow,Mapper).CreateAsync(fixture.TenantID,new ExhibitionManagementSystem.Models.DTOs.Hall.HallCreateDto {VenueID=target.VenueID,HallName="Concurrent hall"})).IsSuccess;
            }
            var result=await Task.WhenAll(Delete(),Create());seed.ChangeTracker.Clear();
            var deleted=(await seed.Venues.IgnoreQueryFilters().SingleAsync(v=>v.VenueID==target.VenueID)).IsDeleted;
            var hasChild=exhibitionCase?await seed.Exhibitions.AnyAsync(e=>e.VenueID==target.VenueID):await seed.Halls.AnyAsync(h=>h.VenueID==target.VenueID);
            Assert.Equal(1,result.Count(success=>success));Assert.False(deleted&&hasChild);
            if(hasChild)
            {
                await using var read=new ApplicationDbContext(options,new Scope(fixture.TenantID));using var readUow=new UnitOfWork(read,WorkflowFixture.Actor(fixture.TenantID));
                if(exhibitionCase)Assert.True((await new ExhibitionService(readUow,Mapper).GetByIdAsync(fixture.TenantID,(await seed.Exhibitions.SingleAsync(e=>e.VenueID==target.VenueID)).ExhibitionID)).IsSuccess);
                else Assert.True((await new HallService(readUow,Mapper).GetByIdAsync(fixture.TenantID,(await seed.Halls.SingleAsync(h=>h.VenueID==target.VenueID)).HallID)).IsSuccess);
            }
        }
        finally{SqlConnection.ClearAllPools();await seed.Database.EnsureDeletedAsync();}
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SqlSponsorshipDeletionCannotRaceWithContractOrAssignment(bool spaceCase)
    {
        var name="EMS_Feature_SponsorRace_"+Guid.NewGuid().ToString("N");
        var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION")){InitialCatalog=name,TrustServerCertificate=true};
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var seed=new ApplicationDbContext(options,new WorkflowFixture.BypassContext());
        try
        {
            await seed.Database.MigrateAsync();var fixture=await WorkflowFixture.SeedAsync(seed);
            var exhibition=await seed.Exhibitions.SingleAsync();
            var sponsor=new Sponsor{TenantID=fixture.TenantID,CompanyName="Race sponsor"};
            var package=new SponsorshipPackage{TenantID=fixture.TenantID,ExhibitionID=exhibition.ExhibitionID,PackageName="Race package",CurrencyCode="LYD",MaxSponsorsCount=1};
            var space=new AdvertisingSpace{TenantID=fixture.TenantID,ExhibitionID=exhibition.ExhibitionID,SpaceName="Race space",SpaceCode="RACE",CurrencyCode="LYD",IsAvailable=true};
            var contract=new SponsorshipContract{TenantID=fixture.TenantID,ExhibitionID=exhibition.ExhibitionID,Sponsor=sponsor,ContractNumber="EXISTING",CurrencyCode="LYD",Status=SponsorshipStatus.Confirmed,CreatedByUserId="workflow-user"};
            seed.AddRange(package,space,contract);await seed.SaveChangesAsync();
            var barrier=new ParentReadBarrier(spaceCase?"AdvertisingSpaces":"SponsorshipPackages");
            var racing=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).AddInterceptors(barrier).Options;
            async Task<bool> Delete()
            {
                await using var db=new ApplicationDbContext(racing,new Scope(fixture.TenantID));using var uow=new UnitOfWork(db,WorkflowFixture.Actor(fixture.TenantID));var service=new SponsorshipService(uow,Mapper);
                return (spaceCase?await service.DeleteSpaceAsync(fixture.TenantID,space.SpaceID,"workflow-user"):await service.DeletePackageAsync(fixture.TenantID,package.PackageID,"workflow-user")).IsSuccess;
            }
            async Task<bool> Create()
            {
                await using var db=new ApplicationDbContext(racing,new Scope(fixture.TenantID));using var uow=new UnitOfWork(db,WorkflowFixture.Actor(fixture.TenantID));var service=new SponsorshipService(uow,Mapper);
                return spaceCase
                    ?(await service.AssignSpaceAsync(fixture.TenantID,new ExhibitionManagementSystem.Models.DTOs.Sponsorship.SponsorAdAssignmentCreateDto{ContractID=contract.ContractID,SpaceID=space.SpaceID})).IsSuccess
                    :(await service.CreateContractAsync(fixture.TenantID,"workflow-user",new ExhibitionManagementSystem.Models.DTOs.Sponsorship.SponsorshipContractCreateDto{ExhibitionID=exhibition.ExhibitionID,SponsorID=sponsor.SponsorID,PackageID=package.PackageID,ContractNumber="NEW",CurrencyCode="LYD",TotalAmount=100})).IsSuccess;
            }
            var results=await Task.WhenAll(Delete(),Create());Assert.Equal(1,results.Count(success=>success));seed.ChangeTracker.Clear();
            var deleted=spaceCase?(await seed.AdvertisingSpaces.IgnoreQueryFilters().SingleAsync()).IsDeleted:(await seed.SponsorshipPackages.IgnoreQueryFilters().SingleAsync()).IsDeleted;
            var child=spaceCase?await seed.SponsorAdAssignments.AnyAsync(a=>a.SpaceID==space.SpaceID):await seed.SponsorshipContracts.AnyAsync(c=>c.PackageID==package.PackageID);
            Assert.False(deleted&&child);
        }
        finally{SqlConnection.ClearAllPools();await seed.Database.EnsureDeletedAsync();}
    }

    [SqlServerTheory]
    [InlineData("DeletePackage")]
    [InlineData("DeleteSpace")]
    [InlineData("DeletePackageHistory")]
    [InlineData("DeleteSpaceHistory")]
    [InlineData("SchedulesAndHalls")]
    [InlineData("TenantPlan")]
    [InlineData("PricingPrecision")]
    public async Task SqlFeatureInvariant(string scenario)
    {
        var name="EMS_Feature_Audit_"+Guid.NewGuid().ToString("N");
        var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION")) {InitialCatalog=name,TrustServerCertificate=true};
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var seed=new ApplicationDbContext(options,new WorkflowFixture.BypassContext());
        try
        {
            await seed.Database.MigrateAsync(); var fixture=await WorkflowFixture.SeedAsync(seed,InvoiceStatus.Draft);
            var exhibition=await seed.Exhibitions.SingleAsync(); exhibition.StartDate=DateTime.UtcNow.Date; exhibition.EndDate=DateTime.UtcNow.Date.AddDays(3);
            var package=new SponsorshipPackage {TenantID=fixture.TenantID,ExhibitionID=exhibition.ExhibitionID,PackageName="Purchased Gold",CurrencyCode="LYD",Price=750,MaxSponsorsCount=3};
            var space=new AdvertisingSpace {TenantID=fixture.TenantID,ExhibitionID=exhibition.ExhibitionID,SpaceCode="A1",SpaceName="Paid billboard",CurrencyCode="LYD",IsAvailable=false};
            var contract=new SponsorshipContract {TenantID=fixture.TenantID,ExhibitionID=exhibition.ExhibitionID,
                Sponsor=new Sponsor {TenantID=fixture.TenantID,CompanyName="Synthetic sponsor"},Package=package,
                ContractNumber="AUDIT-1",CurrencyCode="LYD",TotalAmount=750,Status=SponsorshipStatus.Confirmed,CreatedByUserId="workflow-user"};
            contract.IsDeleted=scenario=="DeletePackageHistory";
            contract.AdAssignments.Add(new SponsorAdAssignment {Space=space,IsDeleted=scenario=="DeleteSpaceHistory"}); seed.SponsorshipContracts.Add(contract);
            seed.ExhibitionSchedules.Add(new ExhibitionSchedule {ExhibitionID=exhibition.ExhibitionID,HallID=1,EventName="Real hall",StartDateTime=DateTime.UtcNow,EndDateTime=DateTime.UtcNow.AddHours(1)});
            seed.TenantSubscriptions.Add(new TenantSubscription {TenantID=fixture.TenantID,Plan="Enterprise",CurrencyCode="LYD",StartDate=DateTime.UtcNow.Date.AddDays(-1),EndDate=DateTime.UtcNow.Date.AddDays(30),Status=SubscriptionStatus.Active});
            await seed.SaveChangesAsync();
            await using var db=new ApplicationDbContext(options,new Scope(fixture.TenantID));
            using var uow=new UnitOfWork(db,WorkflowFixture.Actor(fixture.TenantID));
            if(scenario.StartsWith("Delete",StringComparison.Ordinal))
            {
                var service=new SponsorshipService(uow,Mapper); var before=await service.GetContractByIdAsync(fixture.TenantID,contract.ContractID);
                var packageCase=scenario.StartsWith("DeletePackage",StringComparison.Ordinal);
                var deleted=packageCase?await service.DeletePackageAsync(fixture.TenantID,package.PackageID,"workflow-user"):await service.DeleteSpaceAsync(fixture.TenantID,space.SpaceID,"workflow-user");
                db.ChangeTracker.Clear(); var after=await service.GetContractByIdAsync(fixture.TenantID,contract.ContractID);
                Assert.False(deleted.IsSuccess);Assert.Equal(packageCase?"PACKAGE_HAS_CONTRACTS":"SPACE_HAS_ASSIGNMENTS",deleted.ErrorCode);
                Assert.False((await db.SponsorshipPackages.IgnoreQueryFilters().SingleAsync()).IsDeleted);
                Assert.False((await db.AdvertisingSpaces.IgnoreQueryFilters().SingleAsync()).IsDeleted);
                if(before.IsSuccess){Assert.True(after.IsSuccess);Assert.Equal(before.Data!.PackageName,after.Data!.PackageName);Assert.Equal(before.Data.AdAssignments.Count,after.Data.AdAssignments.Count);}
            }
            else if(scenario=="SchedulesAndHalls")
            {
                var schedules=await new ExhibitionService(uow,Mapper).GetSchedulesAsync(fixture.TenantID,exhibition.ExhibitionID);
                var halls=await new HallService(uow,Mapper).GetByVenueAsync(fixture.TenantID,exhibition.VenueID);
                Record(scenario,new {scheduleHall=schedules.Data![0].HallName,hallName=halls.Data![0].HallName,boothsCount=halls.Data[0].BoothsCount,storedBooths=await db.Booths.CountAsync()});
                Assert.Equal("Hall",schedules.Data[0].HallName); Assert.Equal(1,halls.Data[0].BoothsCount); Assert.Equal(1,await db.Booths.CountAsync());
            }
            else if(scenario=="TenantPlan")
            {
                var service=new TenantService(uow,Mapper,db,null!,null!,null!,Options.Create(new TenantInvitationOptions()),NullLogger<TenantService>.Instance);
                var list=await service.GetAllAsync(1,20); var active=await service.GetActiveSubscriptionAsync(fixture.TenantID);
                Record(scenario,new {listPlan=list.Data!.Items[0].CurrentPlan,activePlan=active.Data!.PlanName});
                Assert.Equal("Enterprise",list.Data.Items[0].CurrentPlan); Assert.Equal("Enterprise",active.Data.PlanName);
                db.ChangeTracker.Clear();
                Assert.Equal("Enterprise",(await service.GetByIdAsync(fixture.TenantID)).Data!.CurrentPlan);
            }
            else if(scenario=="PricingPrecision")
            {
                var pricing=new PricingService(uow,Mapper);
                var rule=await pricing.CreateBoothPriceRuleAsync(fixture.TenantID,new BoothPriceRuleCreateDto {PricePerSqM=.02m,CurrencyCode="LYD",ValidFrom=DateTime.UtcNow.Date});
                var services=new ServiceManagementService(uow,Mapper);
                var svc=await services.CreateAsync(fixture.TenantID,new ServiceCreateDto {ServiceName="Valid price",DefaultPrice=.02m,DefaultCurrencyCode="LYD"});
                Assert.True(rule.IsSuccess,rule.ErrorMessage);Assert.True(svc.IsSuccess,svc.ErrorMessage);
                foreach(var amount in new[]{.015m,decimal.MaxValue,-1m})
                {
                    var invalidRule=new BoothPriceRuleCreateDto {PricePerSqM=amount,CurrencyCode="LYD",ValidFrom=DateTime.UtcNow.Date};
                    Assert.Equal("INVALID_PRICE",(await pricing.CreateBoothPriceRuleAsync(fixture.TenantID,invalidRule)).ErrorCode);
                    Assert.Equal("INVALID_PRICE",(await pricing.UpdateBoothPriceRuleAsync(fixture.TenantID,rule.Data!.RuleID,invalidRule)).ErrorCode);
                    var invalidService=new ServiceCreateDto {ServiceName="Invalid",DefaultPrice=amount,DefaultCurrencyCode="LYD"};
                    Assert.Equal("INVALID_PRICE",(await services.CreateAsync(fixture.TenantID,invalidService)).ErrorCode);
                    Assert.Equal("INVALID_PRICE",(await services.UpdateAsync(fixture.TenantID,svc.Data!.ServiceID,invalidService)).ErrorCode);
                    Assert.Equal("INVALID_PRICE",(await pricing.CreateServicePriceRuleAsync(fixture.TenantID,new ServicePriceRuleCreateDto {ServiceID=svc.Data.ServiceID,UnitPrice=amount,CurrencyCode="LYD",ValidFrom=DateTime.UtcNow.Date})).ErrorCode);
                }
                db.ChangeTracker.Clear(); var storedRule=await db.BoothPriceRules.SingleAsync(); var storedService=await db.Services.SingleAsync();
                Assert.Equal(rule.Data!.PricePerSqM,storedRule.PricePerSqM);Assert.Equal(svc.Data!.DefaultPrice,storedService.DefaultPrice);
                Assert.Empty(await db.ServicePriceRules.ToListAsync());
            }
        }
        finally {SqlConnection.ClearAllPools();await seed.Database.EnsureDeletedAsync();}
    }
    sealed class Host(WebApplication app,HttpClient client):IAsyncDisposable
    {
        public HttpClient Client=>client;
        public static async Task<Host> Start(InvoiceStatus status=InvoiceStatus.Draft)
        {
            var b=WebApplication.CreateBuilder(new WebApplicationOptions {EnvironmentName="Development"});b.Configuration["AllowedHosts"]="127.0.0.1";b.Logging.ClearProviders();b.WebHost.UseUrls("http://127.0.0.1:0");
            b.Services.AddHttpContextAccessor();b.Services.AddScoped<ITenantContext>(_=>new Scope(1));
            var name="EMS_Feature_Http_Audit_"+Guid.NewGuid().ToString("N");
            var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION")) {InitialCatalog=name,TrustServerCertificate=true};
            b.Services.AddDbContext<ApplicationDbContext>(o=>o.UseSqlServer(connection.ConnectionString));
            b.Services.AddScoped<IUnitOfWork,UnitOfWork>();b.Services.AddSingleton(Mapper);
            b.Services.AddScoped<IReservationService,ExhibitionManagementSystem.Services.Implementations.ReservationService>();
            b.Services.AddScoped<IPricingService,PricingService>();b.Services.AddScoped<IFinancialService,FinancialService>();
            b.Services.AddScoped<ISponsorshipService,SponsorshipService>();b.Services.AddScoped<IExhibitorService,ExhibitorService>();
            b.Services.AddScoped<IExhibitionService,ExhibitionService>();b.Services.AddScoped<IHallService,HallService>();
            b.Services.AddScoped<ITicketService,TicketService>();b.Services.AddScoped<IBadgeService,BadgeService>();
            b.Services.AddAuthentication("Probe").AddScheme<AuthenticationSchemeOptions,ProbeAuth>("Probe",_=>{});b.Services.AddAuthorization(AuthorizationPolicies.Configure);
            b.Services.AddControllers().AddApplicationPart(typeof(TicketsController).Assembly);
            var app=b.Build();
            using(var scope=app.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                // Test-only bypass for fixture creation. All HTTP requests use tenant scope.
                var opts=scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                await using var seed=new ApplicationDbContext(opts,new WorkflowFixture.BypassContext());
                await seed.Database.MigrateAsync();var invoice=await WorkflowFixture.SeedAsync(seed,status);var exhibition=await seed.Exhibitions.SingleAsync();
                exhibition.StartDate=DateTime.UtcNow.Date;exhibition.EndDate=DateTime.UtcNow.Date.AddDays(3);
                seed.Booths.Add(new Booth {TenantID=1,HallID=1,BoothNumber="B-2",CurrentAreaSqM=10,OriginalAreaSqM=10});
                seed.BoothPriceRules.Add(new BoothPriceRule {TenantID=1,PricePerSqM=10,CurrencyCode="LYD",ValidFrom=DateTime.UtcNow.Date});
                seed.SponsorshipContracts.Add(new SponsorshipContract {TenantID=1,ExhibitionID=1,Sponsor=new Sponsor {TenantID=1,CompanyName="Actual sponsor"},ContractNumber="WEB-1",CurrencyCode="LYD",TotalAmount=750,CreatedByUserId="workflow-user"});
                seed.Tickets.Add(new Ticket {TenantID=1,ExhibitionID=1,QRCode="AUDIT-TICKET",TicketType="Regular",CurrencyCode="LYD",Status=TicketStatus.Active,Visitor=new Visitor {TenantID=1,FullName="Synthetic attendee"}});
                await seed.SaveChangesAsync();
            }
            app.UseExceptionHandler(handler=>handler.Run(ctx=>{ctx.Response.StatusCode=500;return Task.CompletedTask;}));
            app.UseAuthentication();app.UseAuthorization();app.UseMiddleware<IdempotencyMiddleware>();app.MapControllers();await app.StartAsync();
            var url=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var client=new HttpClient(new HttpClientHandler {UseProxy=false}) {BaseAddress=new Uri(url)};client.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString("N"));return new Host(app,client);
        }
        public async ValueTask DisposeAsync(){client.Dispose();await app.StopAsync();using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();SqlConnection.ClearAllPools();await db.Database.EnsureDeletedAsync();}await app.DisposeAsync();}
    }
    sealed class ProbeAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role=Request.Headers["Probe-Role"].FirstOrDefault()??"TenantAdmin";
            var principal=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"workflow-user"),new Claim("TenantId","1"),new Claim(ClaimTypes.Role,role)],Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal,Scheme.Name)));
        }
    }
}

