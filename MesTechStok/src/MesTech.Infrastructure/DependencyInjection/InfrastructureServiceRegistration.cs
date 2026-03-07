using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Infrastructure.AI;
using MesTech.Infrastructure.Caching;
using MesTech.Infrastructure.HealthChecks;
using MesTech.Infrastructure.Jobs;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Persistence;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Infrastructure.Security;
using MesTech.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Tenant & User — Development ortami
        services.AddSingleton<ITenantProvider, DevelopmentTenantProvider>();
        services.AddSingleton<ICurrentUserService, DevelopmentUserService>();

        // AuditInterceptor
        services.AddScoped<AuditInterceptor>();

        // DbContext — PostgreSQL
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
            });
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

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain Events
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Domain Services
        services.AddSingleton<StockCalculationService>();
        services.AddSingleton<PricingService>();
        services.AddSingleton<BarcodeValidationService>();

        // Encryption
        var encryptionKey = configuration["Security:EncryptionKey"]
            ?? AesGcmEncryptionService.GenerateKey();
        services.AddSingleton(new AesGcmEncryptionService(encryptionKey));

        // === FAZ 1: ALTYAPI AKTIFLESTIRME ===

        // Redis Cache
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:Configuration"]
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "MesTech_";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // RabbitMQ MassTransit Event Bus
        services.AddMesTechMessaging(configuration);
        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();

        // === MESA OS Bridge (Dalga 1: Mock, Dalga 2: Monitoring) ===
        services.AddScoped<IMesaAIService, MockMesaAIService>();
        services.AddScoped<IMesaBotService, MockMesaBotService>();
        services.AddScoped<IMesaEventPublisher, MesaEventPublisher>();
        services.AddSingleton<IMesaEventMonitor, MesaEventMonitor>();

        // MESA Status Endpoint (http://localhost:5101/api/mesa/status)
        services.AddHostedService(sp =>
            new MesaStatusEndpoint(
                sp.GetRequiredService<IMesaEventMonitor>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MesaStatusEndpoint>>()));

        // Hangfire Background Jobs
        services.AddMesTechHangfire(configuration);

        // === DALGA 2: Offline Mode (iskelet — Dalga 3'te tamamlanacak) ===
        services.AddScoped<IOfflineQueue, OfflineQueueService>();
        services.AddScoped<ISyncManager, SyncManagerService>();
        services.AddSingleton<IConnectivityService, ConnectivityService>();

        // Data Seeder
        services.AddScoped<DataSeeder>();

        // HealthCheck
        services.AddSingleton<PlatformHealthCheckService>();
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis")
            .AddCheck<PostgresHealthCheck>("postgresql");

        // Health Check HTTP Endpoint (http://localhost:5100/health)
        services.AddHostedService(sp =>
            new HealthCheckEndpoint(
                sp.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HealthCheckEndpoint>>()));

        // === Integration Layer (Adapters, Factory, Orchestrator) ===
        services.AddIntegrationServices();

        return services;
    }
}
