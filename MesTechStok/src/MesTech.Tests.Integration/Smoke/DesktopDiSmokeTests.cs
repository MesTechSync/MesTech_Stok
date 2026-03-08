using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Infrastructure.AI;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Persistence;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Infrastructure.Security;
using MesTech.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CoreAppDbContext = MesTechStok.Core.Data.AppDbContext;

namespace MesTech.Tests.Integration.Smoke;

/// <summary>
/// Desktop DI full smoke test — mirrors App.xaml.cs ConfigureServices.
/// Tests BOTH Core.Data.AppDbContext (old) and Infrastructure.Persistence.AppDbContext (new)
/// plus all service registrations the Desktop app needs.
/// Emirname G1: DI resolution smoke test (tum servisler).
/// </summary>
[Trait("Category", "Smoke")]
[Trait("Category", "Integration")]
public class DesktopDiSmokeTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public DesktopDiSmokeTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // In-memory configuration (mirrors App.xaml.cs fallback)
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
            ["Database:Provider"] = "PostgreSQL",
            ["Resilience:MaxRetries"] = "3",
            ["OpenCartSettings:ApiUrl"] = "http://localhost",
            ["OpenCartSettings:ApiKey"] = "test-key",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // -- World 1: Core.Data.AppDbContext (old services) --
        services.AddDbContext<CoreAppDbContext>(options =>
            options.UseInMemoryDatabase($"CoreDiSmoke_{Guid.NewGuid()}"));

        // Core Business Services
        services.AddScoped<MesTechStok.Core.Services.Abstract.IProductService, MesTechStok.Core.Services.Concrete.ProductService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.IInventoryService, MesTechStok.Core.Services.Concrete.InventoryService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.IOrderService, MesTechStok.Core.Services.Concrete.OrderService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.ICustomerService, MesTechStok.Core.Services.Concrete.CustomerService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.IStockService, MesTechStok.Core.Services.Concrete.StockService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.ILoggingService, MesTechStok.Core.Services.Concrete.LoggingService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.IQRCodeService, MesTechStok.Core.Services.Concrete.QRCodeService>();

        // -- World 2: Infrastructure.Persistence.AppDbContext (Clean Architecture) --
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase($"InfraDiSmoke_{Guid.NewGuid()}");
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MesTech.Application.Commands.AddStock.AddStockHandler).Assembly));

        // Tenant & User
        services.AddSingleton<ITenantProvider, DevelopmentTenantProvider>();
        services.AddSingleton<ICurrentUserService, DevelopmentUserService>();

        // Clean Architecture Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();

        // UnitOfWork + Events
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IEventPublisher, MesTech.Infrastructure.Messaging.InMemoryEventPublisher>();

        // Domain Services
        services.AddSingleton<StockCalculationService>();
        services.AddSingleton<PricingService>();
        services.AddSingleton<BarcodeValidationService>();

        // Encryption
        services.AddSingleton(new AesGcmEncryptionService(AesGcmEncryptionService.GenerateKey()));

        // MESA OS Mock Services
        services.AddScoped<IMesaAIService, MockMesaAIService>();
        services.AddScoped<IMesaBotService, MockMesaBotService>();
        services.AddSingleton<IMesaEventMonitor, MesaEventMonitor>();

        // Offline Mode
        services.AddScoped<IOfflineQueue, OfflineQueueService>();
        services.AddScoped<ISyncManager, SyncManagerService>();
        services.AddSingleton<IConnectivityService, ConnectivityService>();

        // Integration Layer (Adapters, Factory, Orchestrator, Webhook, TokenCache)
        MesTech.Infrastructure.DependencyInjection.IntegrationServiceRegistration.AddIntegrationServices(services);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = false
        });
    }

    // -- World 1: Core Services --

    [Fact]
    public void CoreAppDbContext_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        var ctx = scope.ServiceProvider.GetService<CoreAppDbContext>();
        ctx.Should().NotBeNull("Core.Data.AppDbContext must resolve for old services");
    }

    [Theory]
    [InlineData(typeof(MesTechStok.Core.Services.Abstract.IProductService))]
    [InlineData(typeof(MesTechStok.Core.Services.Abstract.IInventoryService))]
    [InlineData(typeof(MesTechStok.Core.Services.Abstract.IOrderService))]
    [InlineData(typeof(MesTechStok.Core.Services.Abstract.ICustomerService))]
    [InlineData(typeof(MesTechStok.Core.Services.Abstract.IStockService))]
    [InlineData(typeof(MesTechStok.Core.Services.Abstract.ILoggingService))]
    [InlineData(typeof(MesTechStok.Core.Services.Abstract.IQRCodeService))]
    public void CoreBusinessService_ShouldResolve(Type serviceType)
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.Should().NotBeNull($"{serviceType.Name} must be registered (Core)");
    }

    // -- World 2: Infrastructure Services --

    [Fact]
    public void InfraAppDbContext_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        var ctx = scope.ServiceProvider.GetService<AppDbContext>();
        ctx.Should().NotBeNull("Infrastructure.AppDbContext must resolve for Clean Architecture");
    }

    [Theory]
    [InlineData(typeof(IProductRepository))]
    [InlineData(typeof(IStockMovementRepository))]
    [InlineData(typeof(IWarehouseRepository))]
    [InlineData(typeof(IOrderRepository))]
    [InlineData(typeof(ITenantRepository))]
    [InlineData(typeof(IStoreRepository))]
    [InlineData(typeof(ICategoryRepository))]
    [InlineData(typeof(ISupplierRepository))]
    public void InfraRepository_ShouldResolve(Type serviceType)
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.Should().NotBeNull($"{serviceType.Name} must be registered (Infrastructure)");
    }

    [Fact]
    public void UnitOfWork_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetService<IUnitOfWork>().Should().NotBeNull();
    }

    [Fact]
    public void DomainEventDispatcher_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetService<IDomainEventDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void EventPublisher_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    // -- Domain Services --

    [Fact]
    public void StockCalculationService_ShouldResolve() =>
        _provider.GetService<StockCalculationService>().Should().NotBeNull();

    [Fact]
    public void PricingService_ShouldResolve() =>
        _provider.GetService<PricingService>().Should().NotBeNull();

    [Fact]
    public void BarcodeValidationService_ShouldResolve() =>
        _provider.GetService<BarcodeValidationService>().Should().NotBeNull();

    // -- MESA OS --

    [Fact]
    public void MesaAIService_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetService<IMesaAIService>().Should().NotBeNull();
    }

    [Fact]
    public void MesaBotService_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetService<IMesaBotService>().Should().NotBeNull();
    }

    [Fact]
    public void MesaEventMonitor_ShouldResolve() =>
        _provider.GetService<IMesaEventMonitor>().Should().NotBeNull();

    // -- Offline Mode --

    [Fact]
    public void OfflineQueue_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetService<IOfflineQueue>().Should().NotBeNull();
    }

    [Fact]
    public void SyncManager_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetService<ISyncManager>().Should().NotBeNull();
    }

    [Fact]
    public void ConnectivityService_ShouldResolve() =>
        _provider.GetService<IConnectivityService>().Should().NotBeNull();

    // -- Encryption --

    [Fact]
    public void AesGcmEncryptionService_ShouldResolve() =>
        _provider.GetService<AesGcmEncryptionService>().Should().NotBeNull();

    // -- Both Worlds Coexist --

    [Fact]
    public void BothDbContexts_ShouldResolveIndependently()
    {
        using var scope = _provider.CreateScope();
        var core = scope.ServiceProvider.GetService<CoreAppDbContext>();
        var infra = scope.ServiceProvider.GetService<AppDbContext>();

        core.Should().NotBeNull("Core AppDbContext");
        infra.Should().NotBeNull("Infrastructure AppDbContext");
        core.Should().NotBeSameAs(infra, "Two separate DbContext instances");
    }

    public void Dispose() => _provider?.Dispose();
}
