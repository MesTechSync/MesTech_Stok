using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
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
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Kestrel request size limits — prevent resource exhaustion (KEŞİF-DEV6)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB (bulk import + file upload)
    options.Limits.MaxRequestHeadersTotalSize = 16 * 1024; // 16 KB headers
});

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
    .WriteTo.Seq(ctx.Configuration["Serilog:SeqUrl"] ?? "http://localhost:3343")
    .Enrich.WithProperty("Application", "MesTech.WebApi"));

// JWT Options — bind from appsettings "Jwt" section (E01)
builder.Services.Configure<JwtTokenOptions>(
    builder.Configuration.GetSection(JwtTokenOptions.SectionName));

// API Key authentication (reads ApiSecurity section from appsettings.json)
builder.Services.AddApiKeyAuthentication(builder.Configuration);

// Memory cache for future endpoint use
builder.Services.AddMemoryCache();

// MediatR — Application CQRS handlers + Infrastructure event handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly);
    cfg.RegisterServicesFromAssembly(
        typeof(MesTech.Infrastructure.Integration.Orchestration.StockChangedEventHandler).Assembly);
});

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
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = "100";
        context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = 429,
            detail = "API rate limit exceeded. 100 requests per minute per API key."
        }, ct);
    };
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
    // Stricter rate limit for auth endpoints — 20 req/min per IP (brute force defense layer)
    options.AddPolicy("AuthRateLimit", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
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

// Response compression — Brotli + Gzip for JSON payloads (KEŞİF-DEV6)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);

// Request timeouts — 30s default for API endpoints (KEŞİF-DEV6-T7)
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
});

// Output cache — short-lived cache for stable lookup endpoints (KEŞİF-DEV6-T7)
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Lookup60s", b => b.Expire(TimeSpan.FromSeconds(60)).SetVaryByQuery("*"));
});

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

// Production startup validation — fail fast on missing critical config (KEŞİF-DEV6-T15)
if (app.Environment.IsProduction())
{
    var connStr = app.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connStr))
        throw new InvalidOperationException(
            "STARTUP BLOCKED: ConnectionStrings:DefaultConnection is empty. " +
            "Set via environment variable or user-secrets before deploying to production.");

    var jwtSecret = app.Configuration["Jwt:Secret"];
    if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Contains("CHANGE", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            "STARTUP BLOCKED: Jwt:Secret is placeholder or empty. " +
            "Set a secure 32+ character secret via user-secrets before deploying to production.");
}

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

// HTTPS redirection + HSTS (S01f+S01g security hardening)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// Response compression — before static files and routing (KEŞİF-DEV6)
app.UseResponseCompression();

// Correlation ID — propagate or generate X-Correlation-ID for distributed tracing (KEŞİF-DEV6)
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-ID";
    var correlationId = context.Request.Headers[header].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N");
    context.Items["CorrelationId"] = correlationId;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers[header] = correlationId;
        return Task.CompletedTask;
    });
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

// Security headers (OWASP recommended)
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["X-XSS-Protection"] = "0"; // Modern browsers: CSP preferred over X-XSS-Protection
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
    await next();
});

// Serilog HTTP request logging — structured log per request with timing (KEŞİF-DEV6)
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? string.Empty);
        if (httpContext.Items.TryGetValue("CorrelationId", out var cid))
            diagnosticContext.Set("CorrelationId", cid?.ToString() ?? string.Empty);
        var apiKey = httpContext.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
            diagnosticContext.Set("ApiKeyPrefix", apiKey[..Math.Min(8, apiKey.Length)] + "...");
    };
});

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

// Swagger UI — Development only (production API structure exposure = OWASP risk) (KEŞİF-DEV6-T14)
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MesTech API v1");
        c.RoutePrefix = "swagger";
    });
}

// JWT Authentication + Authorization middleware (G-02 SignalR auth)
app.UseAuthentication();
app.UseAuthorization();

// API Key middleware — bypass paths skip validation (/health, /metrics, /api/webhooks, /hubs)
app.UseApiKeyAuthentication();

// Prometheus HTTP metrics — request count, duration, status codes per endpoint
app.UseHttpMetrics();

// Rate limiter middleware — after auth so API key is available
app.UseRateLimiter();

// Request timeout middleware — cancels long-running requests (KEŞİF-DEV6-T7)
app.UseRequestTimeouts();

// Output cache middleware — after auth, before endpoints (KEŞİF-DEV6-T7)
app.UseOutputCache();

// TenantId validation — reject Guid.Empty to prevent cross-tenant data access (KEŞİF-DEV6-T13)
app.Use(async (context, next) =>
{
    var tenantId = context.Request.Query["tenantId"].FirstOrDefault();
    if (tenantId != null && Guid.TryParse(tenantId, out var parsed) && parsed == Guid.Empty)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Bad Request",
            status = 400,
            detail = "tenantId cannot be empty (Guid.Empty). Provide a valid tenant identifier."
        });
        return;
    }
    await next();
});

// Health + Metrics endpoints (HealthCheckService + Prometheus — no auth bypass paths)
HealthEndpoints.Map(app);

// API v1 endpoints
AuthEndpoints.Map(app);
ProductEndpoints.Map(app);
ProductEndpoints.MapBuybox(app);
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
ShipmentEndpoints.Map(app);
CargoEndpoints.Map(app);
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
SavedReportEndpoints.Map(app);
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
CampaignEndpoints.Map(app);
LoyaltyEndpoints.Map(app);
ReturnEndpoints.Map(app);
BillingEndpoints.Map(app);
FulfillmentEndpoints.Map(app);
HrEndpoints.Map(app);
OnboardingEndpoints.Map(app);
LogEndpoints.Map(app);
SystemEndpoints.Map(app);
ProductImageEndpoints.Map(app);

// SignalR real-time hub (G-02)
app.MapHub<MesTechHub>("/hubs/mestech");

app.Run();

// Required for WebApplicationFactory<Program> in integration tests (E03)
public partial class Program { }
