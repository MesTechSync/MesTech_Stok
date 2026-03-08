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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// DI resolution regression tests — verifies every critical service resolves without error.
/// Registers services manually (mirrors InfrastructureServiceRegistration) with InMemory DB
/// to avoid Hangfire/MassTransit eager connection issues.
/// If a constructor signature changes or a dependency is missing, these tests will catch it.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Regression")]
public class DiResolutionTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public DiResolutionTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MesTech.Application.Commands.AddStock.AddStockHandler).Assembly));

        // Tenant & User
        services.AddSingleton<ITenantProvider, DevelopmentTenantProvider>();
        services.AddSingleton<ICurrentUserService, DevelopmentUserService>();

        // AuditInterceptor
        services.AddScoped<AuditInterceptor>();

        // DbContext — InMemory for DI resolution testing
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase($"DiTest_{Guid.NewGuid()}");
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        // Repositories
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

        // Offline Mode skeleton
        services.AddScoped<IOfflineQueue, OfflineQueueService>();
        services.AddScoped<ISyncManager, SyncManagerService>();
        services.AddSingleton<IConnectivityService, ConnectivityService>();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = false // MediatR handlers needing IIntegratorOrchestrator + MassTransit are external
        });
    }

    // ── Core Repositories ──

    [Theory]
    [InlineData(typeof(IProductRepository))]
    [InlineData(typeof(IStockMovementRepository))]
    [InlineData(typeof(IWarehouseRepository))]
    [InlineData(typeof(IOrderRepository))]
    [InlineData(typeof(ITenantRepository))]
    [InlineData(typeof(IStoreRepository))]
    [InlineData(typeof(ICategoryRepository))]
    [InlineData(typeof(ISupplierRepository))]
    public void Repository_ShouldResolve(Type serviceType)
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.Should().NotBeNull($"{serviceType.Name} should be registered");
    }

    // ── Core Infrastructure Services ──

    [Fact]
    public void UnitOfWork_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
        uow.Should().NotBeNull();
        uow.Should().BeOfType<UnitOfWork>();
    }

    [Fact]
    public void DomainEventDispatcher_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetService<IDomainEventDispatcher>();
        dispatcher.Should().NotBeNull();
    }

    [Fact]
    public void TenantProvider_ShouldResolve()
    {
        var provider = _provider.GetService<ITenantProvider>();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AppDbContext_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetService<AppDbContext>();
        context.Should().NotBeNull();
    }

    // ── Domain Services (Singleton) ──

    [Fact]
    public void StockCalculationService_ShouldResolve()
    {
        var service = _provider.GetService<StockCalculationService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public void PricingService_ShouldResolve()
    {
        var service = _provider.GetService<PricingService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public void BarcodeValidationDomainService_ShouldResolve()
    {
        var service = _provider.GetService<BarcodeValidationService>();
        service.Should().NotBeNull();
    }

    // ── MESA OS Services ──

    [Fact]
    public void MesaAIService_ShouldResolveAsMock()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetService<IMesaAIService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<MockMesaAIService>();
    }

    [Fact]
    public void MesaBotService_ShouldResolveAsMock()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetService<IMesaBotService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<MockMesaBotService>();
    }

    [Fact]
    public void MesaEventMonitor_ShouldResolve()
    {
        var monitor = _provider.GetService<IMesaEventMonitor>();
        monitor.Should().NotBeNull();
    }

    // ── Offline Mode (Dalga 2 skeleton) ──

    [Fact]
    public void OfflineQueue_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        var queue = scope.ServiceProvider.GetService<IOfflineQueue>();
        queue.Should().NotBeNull();
    }

    [Fact]
    public void SyncManager_ShouldResolve()
    {
        using var scope = _provider.CreateScope();
        var sync = scope.ServiceProvider.GetService<ISyncManager>();
        sync.Should().NotBeNull();
    }

    [Fact]
    public void ConnectivityService_ShouldResolve()
    {
        var service = _provider.GetService<IConnectivityService>();
        service.Should().NotBeNull();
    }

    // ── Encryption ──

    [Fact]
    public void AesGcmEncryptionService_ShouldResolve()
    {
        var service = _provider.GetService<AesGcmEncryptionService>();
        service.Should().NotBeNull();
    }

    public void Dispose()
    {
        _provider?.Dispose();
    }
}
