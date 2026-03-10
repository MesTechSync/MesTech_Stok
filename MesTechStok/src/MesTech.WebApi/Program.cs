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

// Remove HealthCheckEndpoint BackgroundService — WebApi provides its own /health and /metrics on port 5100
var healthCheckDescriptor = builder.Services.FirstOrDefault(d =>
    d.ImplementationType?.Name == "HealthCheckEndpoint");
if (healthCheckDescriptor != null)
    builder.Services.Remove(healthCheckDescriptor);

var app = builder.Build();

// API Key middleware — bypass paths skip validation (/health, /metrics)
app.UseApiKeyAuthentication();

// Health + Metrics endpoints (HealthCheckService + Prometheus — no auth bypass paths)
HealthEndpoints.Map(app);

// API v1 endpoints
ProductEndpoints.Map(app);
StockEndpoints.Map(app);
CategoryEndpoints.Map(app);
OrderEndpoints.Map(app);
SyncStatusEndpoints.Map(app);
InvoiceEndpoints.Map(app);
QuotationEndpoints.Map(app);

app.Run();
