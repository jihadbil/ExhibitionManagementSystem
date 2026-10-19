using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Extensions;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Mapping;
using ExhibitionManagementSystem.Services.Extensions;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add database context and repositories
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDataAccess(connectionString);
builder.Services.AddHttpContextAccessor();
var protection = builder.Services.AddDataProtection().SetApplicationName("ExhibitionManagementSystem");
var keyDirectory = builder.Configuration["DataProtection:KeysPath"];
if (builder.Configuration.GetValue<bool>("PasswordReset:Enabled") && (string.IsNullOrWhiteSpace(keyDirectory) || !Path.IsPathRooted(keyDirectory)))
    throw new InvalidOperationException("Password reset delivery requires an absolute persistent DataProtection:KeysPath.");
if (!string.IsNullOrWhiteSpace(keyDirectory)) protection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));
var signingKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Set JwtSettings:SecretKey to a secret containing at least 32 bytes.");

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add custom JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "ExhibitionManagementSystem",
        ValidAudience = jwtSettings["Audience"] ?? "ExhibitionManagementSystemClients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stamp = context.Principal?.FindFirstValue("security_stamp");
            var tenantClaim = context.Principal?.FindFirstValue("TenantId");
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.AsNoTracking().Include(u => u.Tenant)
                .SingleOrDefaultAsync(u => u.Id == id, context.HttpContext.RequestAborted);
            if (user == null || !user.IsActive || !user.Tenant.IsActive
                || string.IsNullOrEmpty(stamp) || stamp != user.SecurityStamp
                || !int.TryParse(tenantClaim, out var tenantId) || tenantId != user.TenantID)
                context.Fail("The session is no longer valid.");
        }
    };
});

builder.Services.AddControllers(options => options.Filters.Add<RequestBoundsFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length > 0)
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});


// OpenAPI مع دعم JWT
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// سياسات التفويض
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",
        policy => policy.RequireRole("Admin", "SuperAdmin"));
    options.AddPolicy("SuperAdminOnly",
        policy => policy.RequireRole("SuperAdmin"));
});

// Register DTO mappings and Service Layer
builder.Services.AddDtoMapping();
builder.Services.AddServiceLayer();

var app = builder.Build();

// A failed schema/bootstrap must prevent this instance from accepting requests.
await DataSeeder.SeedDataAsync(app.Services);
// Configure the HTTP request pipeline.

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors("ConfiguredOrigins");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = new List<string> { "Bearer" }
        };
    });
}

app.MapControllers();

app.Run();

public partial class Program { }

// OpenAPI transformer لدعم JWT في Scalar
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT"
            };

            if (document.Paths != null)
            {
                foreach (var path in document.Paths.Values)
                {
                    if (path?.Operations != null)
                    {
                        foreach (var operation in path.Operations.Values)
                        {
                            if (operation != null)
                            {
                                operation.Security ??= new List<OpenApiSecurityRequirement>();
                                operation.Security.Add(new OpenApiSecurityRequirement
                                {
                                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
