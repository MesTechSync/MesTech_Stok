using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Auth;
using MesTech.Infrastructure.DependencyInjection;
using MesTech.Infrastructure.Middleware;
using MesTech.Infrastructure.Security;
using MesTech.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// JWT Options — bind from appsettings "Jwt" section (E01)
builder.Services.Configure<JwtTokenOptions>(
    builder.Configuration.GetSection(JwtTokenOptions.SectionName));

// API Key authentication (reads ApiSecurity section from appsettings.json)
builder.Services.AddApiKeyAuthentication(builder.Configuration);

// Memory cache for future endpoint use
builder.Services.AddMemoryCache();

// MediatR — Application CQRS handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly));

// Infrastructure (DbContext, Repositories, Domain Services, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Override ITenantProvider for WebApi: JWT claim-based tenant resolution (E02)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, ApiTenantProvider>();

// Remove HealthCheckEndpoint BackgroundService — WebApi provides its own /health and /metrics on port 5100
var healthCheckDescriptor = builder.Services.FirstOrDefault(d =>
    d.ImplementationType?.Name == "HealthCheckEndpoint");
if (healthCheckDescriptor != null)
    builder.Services.Remove(healthCheckDescriptor);

// Rate limiting — 100 requests per minute per API key (multi-tenant friendly)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PerApiKey", context =>
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(apiKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

var app = builder.Build();

// Production: check for pending migrations — auto-migrate YASAK (KOMUTAN KARARI)
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MesTech.Infrastructure.Persistence.AppDbContext>();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count > 0)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("PENDING MIGRATIONS: {Count} migration(s) need manual apply before deploy: {Migrations}",
            pending.Count, string.Join(", ", pending));
    }
}

// API Key middleware — bypass paths skip validation (/health, /metrics)
app.UseApiKeyAuthentication();

// Rate limiter middleware — after auth so API key is available
app.UseRateLimiter();

// Health + Metrics endpoints (HealthCheckService + Prometheus — no auth bypass paths)
HealthEndpoints.Map(app);

// API v1 endpoints
AuthEndpoints.Map(app);
ProductEndpoints.Map(app);
StockEndpoints.Map(app);
CategoryEndpoints.Map(app);
OrderEndpoints.Map(app);
SyncStatusEndpoints.Map(app);
InvoiceEndpoints.Map(app);
QuotationEndpoints.Map(app);
DashboardEndpoints.Map(app);
CrmEndpoints.Map(app);
FinanceEndpoints.Map(app);
SupplierFeedsEndpoints.Map(app);
DropshippingPoolEndpoints.Map(app);
AccountingEndpoints.Map(app);
DropshippingEndpoints.Map(app);
NotificationEndpoints.Map(app);
ShippingEndpoints.Map(app);

app.Run();
