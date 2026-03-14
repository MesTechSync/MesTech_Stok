using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.Jobs;

public static class HangfireConfig
{
    public static IServiceCollection AddMesTechHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? configuration.GetConnectionString("DefaultConnection");

        // Hangfire storage + server
        if (!string.IsNullOrEmpty(connectionString) &&
            connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                });
        }

        // Job'lari register et
        services.AddScoped<TrendyolOrderSyncJob>();
        services.AddScoped<TrendyolStockSyncJob>();
        services.AddScoped<TrendyolPriceSyncJob>();
        services.AddScoped<TrendyolClaimSyncJob>();
        services.AddScoped<OpenCartStockSyncJob>();
        services.AddScoped<InvoiceRetryJob>();
        services.AddScoped<HealthCheckJob>();
        services.AddScoped<SettlementSyncJob>();
        services.AddScoped<CategorySyncJob>();
        services.AddScoped<SupplierFeedSyncJob>();

        // ENT-DROP-IMP-SPRINT-B — DEV 3 Görev B
        services.AddScoped<ReliabilityScoreRecalcJob>();

        // H27 DEV 4 — CRM periyodik job'ları
        services.AddScoped<CrmHangfireJobs>();

        return services;
    }

    /// <summary>
    /// Uygulama basladiginda recurring job'lari register eder.
    /// Her job kendi CronExpression'ini tanimlar.
    /// </summary>
    public static void RegisterRecurringJobs(IServiceProvider? serviceProvider = null)
    {
        RecurringJob.AddOrUpdate<TrendyolOrderSyncJob>(
            "trendyol-order-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<TrendyolStockSyncJob>(
            "trendyol-stock-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/30 * * * *");

        RecurringJob.AddOrUpdate<TrendyolPriceSyncJob>(
            "trendyol-price-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 */6 * * *");

        RecurringJob.AddOrUpdate<TrendyolClaimSyncJob>(
            "trendyol-claim-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/15 * * * *");

        RecurringJob.AddOrUpdate<OpenCartStockSyncJob>(
            "opencart-stock-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/30 * * * *");

        RecurringJob.AddOrUpdate<InvoiceRetryJob>(
            "invoice-retry",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/10 * * * *");

        RecurringJob.AddOrUpdate<HealthCheckJob>(
            "health-check",
            job => job.ExecuteAsync(CancellationToken.None),
            "* * * * *");

        RecurringJob.AddOrUpdate<SettlementSyncJob>(
            "settlement-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 3 * * *");

        RecurringJob.AddOrUpdate<CategorySyncJob>(
            "category-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 4 * * *");

        // Data-driven supplier feed jobs (one per active feed)
        if (serviceProvider != null)
        {
            SupplierFeedSyncJob.RegisterSupplierFeedJobs(serviceProvider);
        }

        // ENT-DROP-IMP-SPRINT-B — DEV 3 Görev B: Güvenilirlik skoru her gece 03:00
        RecurringJob.AddOrUpdate<ReliabilityScoreRecalcJob>(
            "reliability-score-recalc",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(3));

        // H27 DEV 4 — CRM periyodik job'ları
        // Her gece 02:00 — lead süresi kontrolü
        RecurringJob.AddOrUpdate<CrmHangfireJobs>(
            "crm-overdue-leads",
            job => job.CheckOverdueLeadsAsync(),
            "0 2 * * *");

        // Her gece 03:00 — görev süresi kontrolü
        RecurringJob.AddOrUpdate<CrmHangfireJobs>(
            "crm-overdue-tasks",
            job => job.CheckOverdueTasksAsync(),
            "0 3 * * *");
    }
}
