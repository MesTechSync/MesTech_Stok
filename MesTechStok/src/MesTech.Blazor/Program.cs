using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using MesTech.Blazor.Components;
using MesTech.Blazor.Services;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.DependencyInjection;

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

// ── MediatR — Application CQRS handlers (same assembly as WPF + WebApi) ──
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly));

// ── Infrastructure (DbContext, Repositories, Domain Services, Cache, Messaging, etc.) ──
builder.Services.AddInfrastructure(builder.Configuration);

// ── Override ICurrentUserService for Blazor PoC (scoped — per-circuit) ──
builder.Services.AddScoped<ICurrentUserService, BlazorCurrentUserService>();

// ── Notification service (scoped per-circuit for real-time bell) ──
builder.Services.AddScoped<IBlazorNotificationService, BlazorNotificationService>();

// ── MesTechApiClient (HttpClient → WebAPI, scoped per-circuit) ──
builder.Services.AddHttpClient<MesTechApiClient>();

// ── Authentication & Authorization (JWT token-based, scoped per circuit) ──
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ── Onboarding ──
builder.Services.AddScoped<OnboardingService>();

// ── Remove hosted services that conflict with Blazor (HealthCheckEndpoint, MesaStatusEndpoint, RealtimeDashboard) ──
// Blazor has its own HTTP pipeline; standalone TCP listeners on 3100/3101/3102 are WPF-only.
var hostedServicesToRemove = builder.Services
    .Where(d => d.ImplementationType?.Name is "HealthCheckEndpoint" or "MesaStatusEndpoint" or "RealtimeDashboardEndpoint")
    .ToList();
foreach (var descriptor in hostedServicesToRemove)
    builder.Services.Remove(descriptor);

// Port comes from launchSettings.json (5200) or ASPNETCORE_URLS env var — no hardcoded override

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
