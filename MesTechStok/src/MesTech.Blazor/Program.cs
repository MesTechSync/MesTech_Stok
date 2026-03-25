using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using MesTech.Blazor.Components;
using MesTech.Blazor.Services;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.DependencyInjection;

using Polly;
using Polly.Extensions.Http;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog structured logging — Console + Seq (production observability)
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Serilog:SeqUrl"] ?? "http://localhost:3343")
    .Enrich.WithProperty("Application", "MesTech.Blazor"));

// Development: skip DI validation (some repos not yet implemented)
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = false;
        options.ValidateOnBuild = false;
    });
}

// ── Blazor Server ──
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Localization (i18n — Strings.tr.resx / Strings.en.resx) ──
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "tr", "en" };
    options.SetDefaultCulture("tr");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders.Insert(0,
        new CookieRequestCultureProvider { CookieName = ".MesTech.Culture" });
});

// ── MediatR — Application CQRS handlers + Infrastructure event handlers ──
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly);
    cfg.RegisterServicesFromAssembly(
        typeof(MesTech.Infrastructure.Integration.Orchestration.StockChangedEventHandler).Assembly);
});

// ── Infrastructure (DbContext, Repositories, Domain Services, Cache, Messaging, etc.) ──
builder.Services.AddInfrastructure(builder.Configuration, skipSelfHostedEndpoints: true);

// ── Override ICurrentUserService for Blazor PoC (scoped — per-circuit) ──
builder.Services.AddScoped<ICurrentUserService, BlazorCurrentUserService>();

// ── Notification service (scoped per-circuit for real-time bell) ──
builder.Services.AddScoped<IBlazorNotificationService, BlazorNotificationService>();

// ── MesTechApiClient (HttpClient → WebAPI, scoped per-circuit) ──
// Polly resilience: retry 3x (exponential backoff) + circuit breaker (5 fail / 30s break)
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient<MesTechApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:3100");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// ── Authentication & Authorization (JWT token-based, scoped per circuit) ──
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthentication();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ── Onboarding ──
builder.Services.AddScoped<OnboardingService>();

// ── Health Checks (Docker + Kubernetes + Prometheus /health endpoint) ──
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: new[] { "db", "ready" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:3679",
        name: "redis",
        tags: new[] { "cache", "ready" })
    .AddRabbitMQ(
        sp =>
        {
            var factory = new RabbitMQ.Client.ConnectionFactory
            {
                HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest",
                Password = builder.Configuration["RabbitMQ:Password"] ?? "guest"
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        },
        name: "rabbitmq",
        tags: new[] { "messaging", "ready" });

// ── Response Compression (Blazor Server JS bundles + SignalR) ──
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }); // SignalR WebSocket frames
});

// ── Remove hosted services that conflict with Blazor (HealthCheckEndpoint, MesaStatusEndpoint, RealtimeDashboard) ──
// Blazor has its own HTTP pipeline; standalone TCP listeners on 3100/3101/3102 are WPF-only.
var hostedServicesToRemove = builder.Services
    .Where(d => d.ImplementationType?.Name is "HealthCheckEndpoint" or "MesaStatusEndpoint" or "RealtimeDashboardEndpoint")
    .ToList();
foreach (var descriptor in hostedServicesToRemove)
    builder.Services.Remove(descriptor);

// ── Rate Limiting (anti-brute-force login + general throttle) ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: fixed window — 5 requests / 1 minute per IP
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // General: sliding window — 100 requests / minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            }));
});

// Port comes from launchSettings.json (5200) or ASPNETCORE_URLS env var — no hardcoded override

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRateLimiter();
app.UseAntiforgery();

// Health endpoint — Docker healthcheck + Prometheus + load balancer
app.MapHealthChecks("/health");

// Login rate limit — apply "login" policy to /login path
app.MapGet("/login", () => Results.Redirect("/login")).RequireRateLimiting("login");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
