using Microsoft.AspNetCore.Components.Authorization;
using MesTech.Blazor.Components;
using MesTech.Blazor.Services;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Server ──
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── MediatR — Application CQRS handlers (same assembly as WPF + WebApi) ──
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly));

// ── Infrastructure (DbContext, Repositories, Domain Services, Cache, Messaging, etc.) ──
builder.Services.AddInfrastructure(builder.Configuration);

// ── Override ICurrentUserService for Blazor PoC (scoped — per-circuit) ──
builder.Services.AddScoped<ICurrentUserService, BlazorCurrentUserService>();

// ── Authentication & Authorization (JWT token-based, scoped per circuit) ──
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ── Remove hosted services that conflict with Blazor (HealthCheckEndpoint, MesaStatusEndpoint, RealtimeDashboard) ──
// Blazor has its own HTTP pipeline; standalone TCP listeners on 5100/5101/5102 are WPF-only.
var hostedServicesToRemove = builder.Services
    .Where(d => d.ImplementationType?.Name is "HealthCheckEndpoint" or "MesaStatusEndpoint" or "RealtimeDashboardEndpoint")
    .ToList();
foreach (var descriptor in hostedServicesToRemove)
    builder.Services.Remove(descriptor);

// ── Port 5200 ──
builder.WebHost.UseUrls("http://localhost:5200");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
