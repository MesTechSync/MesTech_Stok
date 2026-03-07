using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
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

        return services;
    }
}
