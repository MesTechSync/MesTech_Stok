using MesTech.Infrastructure.DependencyInjection;
using MesTech.Infrastructure.Middleware;
using MesTech.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// API Key middleware — bypass paths skip validation (/health, /metrics)
app.UseApiKeyAuthentication();

// Health endpoint (no auth — bypass path)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

// Metrics placeholder (no auth — bypass path)
app.MapGet("/metrics", () => Results.Text(
    "# MesTech WebApi metrics placeholder\n", "text/plain"));

// API v1 endpoints
ProductEndpoints.Map(app);
StockEndpoints.Map(app);

app.Run();
