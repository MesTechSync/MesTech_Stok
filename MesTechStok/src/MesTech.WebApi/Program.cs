using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Auth;
using MesTech.Infrastructure.DependencyInjection;
using MesTech.Infrastructure.Middleware;
using MesTech.Infrastructure.Persistence;
using MesTech.Infrastructure.Security;
using MesTech.WebApi.Endpoints;
using MesTech.WebApi.Hubs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Development: skip DI validation (some repos not yet implemented)
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = false;
        options.ValidateOnBuild = false;
    });
}

// Serilog structured logging — Console + Seq (Sprint 3)
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Serilog:SeqUrl"] ?? "http://localhost:5341")
    .Enrich.WithProperty("Application", "MesTech.WebApi"));

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
// skipSelfHostedEndpoints=true: HealthCheckEndpoint/MesaStatusEndpoint/RealtimeDashboardEndpoint
// are WPF-only. WebAPI provides /health and /metrics via Kestrel.
builder.Services.AddInfrastructure(builder.Configuration, skipSelfHostedEndpoints: true);

// Override ITenantProvider for WebApi: JWT claim-based tenant resolution (E02)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, ApiTenantProvider>();

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

// ProblemDetails — RFC 7807 compliant error responses (A-M2-06)
builder.Services.AddProblemDetails();

// Swagger/OpenAPI — API documentation with API Key auth support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MesTech API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS — environment-aware production config (A-M2-08)
builder.Services.AddCors(options => options.AddPolicy("SaaS", policy =>
{
    if (builder.Environment.IsDevelopment())
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    }
    else
    {
        policy.WithOrigins("https://mestech.tr", "https://panel.mestech.tr", "https://app.mestech.tr")
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
              .WithHeaders("Authorization", "Content-Type", "X-API-Key")
              .AllowCredentials();
    }
}));

// SignalR real-time bildirim hub'i (G-02)
builder.Services.AddSignalR();

// JWT SignalR auth — query string token support
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // SignalR sends JWT via query string: ?access_token=xxx
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/mestech"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Production: check for pending migrations — auto-migrate YASAK (KOMUTAN KARARI)
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count > 0)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("PENDING MIGRATIONS: {Count} migration(s) need manual apply before deploy: {Migrations}",
            pending.Count, string.Join(", ", pending));
    }
}

// Demo data seeder — populates DB with demo tenant, products, orders on first run (Sprint 3)
try
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await seeder.SeedAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "DemoDataSeeder failed — continuing startup");
}

// Ahmet Bey 14-step demo scenario — realistic end-to-end flow (A-M3-03)
try
{
    using var scope = app.Services.CreateScope();
    var ahmetSeeder = scope.ServiceProvider.GetRequiredService<AhmetBeyDemoSeeder>();
    await ahmetSeeder.SeedAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "AhmetBeyDemoSeeder failed — continuing startup");
}

// Serilog HTTP request logging — structured log per request
app.UseSerilogRequestLogging();

// CORS middleware — before auth and exception handler
app.UseCors("SaaS");

// Global exception handler — RFC 7807 ProblemDetails response (A-M2-06)
app.UseExceptionHandler(error =>
{
    error.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exceptionFeature?.Error, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = app.Environment.IsDevelopment()
                ? exceptionFeature?.Error?.ToString()
                : "An error occurred while processing your request."
        };

        if (app.Environment.IsDevelopment() && exceptionFeature?.Error != null)
        {
            problem.Extensions["stackTrace"] = exceptionFeature.Error.StackTrace;
            problem.Extensions["source"] = exceptionFeature.Error.Source;
        }

        await context.Response.WriteAsJsonAsync(problem);
    });
});

// Swagger JSON spec — all environments for external tool consumption (A-M2-05)
app.UseSwagger();

// Swagger UI — all environments (API documentation accessible, secured by API key auth)
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MesTech API v1");
    c.RoutePrefix = "swagger";
});

// JWT Authentication + Authorization middleware (G-02 SignalR auth)
app.UseAuthentication();
app.UseAuthorization();

// API Key middleware — bypass paths skip validation (/health, /metrics, /api/webhooks, /hubs)
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
DashboardSummaryEndpoint.Map(app);
CrmEndpoints.Map(app);
CrmDashboardEndpoint.Map(app);
CrmMessagesEndpoint.Map(app);
CrmLeadsEndpoint.Map(app);
CrmCustomersEndpoint.Map(app);
FinanceEndpoints.Map(app);
SupplierFeedsEndpoints.Map(app);
DropshippingPoolEndpoints.Map(app);
AccountingEndpoints.Map(app);
DropshippingEndpoints.Map(app);
NotificationEndpoints.Map(app);
ShippingEndpoints.Map(app);
SocialFeedEndpoints.Map(app);
PaymentEndpoints.Map(app);
SeedEndpoints.Map(app);
WarehouseEndpoints.Map(app);
CalendarEndpoints.Map(app);
ProjectEndpoints.Map(app);
BarcodeEndpoints.Map(app);
TenantEndpoints.Map(app);
StoreEndpoints.Map(app);
StoreCredentialEndpoints.Map(app);
PlatformListEndpoint.Map(app);
PlatformSyncEndpoint.Map(app);
CategoryMappingEndpoint.Map(app);
DropshipDashboardEndpoint.Map(app);
DropshipProfitEndpoint.Map(app);
FeedPreviewEndpoint.Map(app);
ProductFetchEndpoint.Map(app);
SystemHealthEndpoints.Map(app);
EInvoiceEndpoints.Map(app);
SandboxEndpoints.Map(app);
DashboardWidgetEndpoints.Map(app);
ReportEndpoints.Map(app);
WebhookEndpoints.Map(app);
IncomeEndpoints.Map(app);
SalaryEndpoints.Map(app);
FixedExpenseEndpoints.Map(app);
PenaltyEndpoints.Map(app);
TaxRecordEndpoints.Map(app);
CariHesapEndpoints.Map(app);
BaBsEndpoints.Map(app);
FixedAssetEndpoints.Map(app);
TaxWithholdingEndpoints.Map(app);
ErpEndpoints.Map(app);
SettingsEndpoints.Map(app);
BulkProductEndpoints.Map(app);
NotificationSettingEndpoints.Map(app);
UserNotificationEndpoints.Map(app);

// SignalR real-time hub (G-02)
app.MapHub<MesTechHub>("/hubs/mestech");

app.Run();

// Required for WebApplicationFactory<Program> in integration tests (E03)
public partial class Program { }
