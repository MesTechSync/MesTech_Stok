using Hangfire;
using Hangfire.PostgreSql;
using MesTech.Infrastructure.Integration.Jobs;
using MesTech.Infrastructure.Jobs.Accounting;
using MesTech.Infrastructure.Jobs.Billing;
using MesTech.Infrastructure.Jobs.Crm;
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

        // Hangfire DI servisleri (IBackgroundJobClient, IRecurringJobManager, vb.)
        if (!string.IsNullOrEmpty(connectionString) &&
            connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHangfire(config =>
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UsePostgreSqlStorage(options =>
                      {
                          options.UseNpgsqlConnection(connectionString);
                      }));
            services.AddHangfireServer();
        }
        else
        {
            // PostgreSQL yoksa sadece IBackgroundJobClient DI kaydını yap; server başlatılmaz.
            services.AddHangfire(config =>
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings());
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

        // Dalga 15 — ERP sync job'ları
        services.AddScoped<ErpStockSyncJob>();
        services.AddScoped<ErpPriceSyncJob>();
        services.AddScoped<ErpAccountSyncJob>();

        // Dalga 10 — SocialFeedRefreshJob (registered via IntegrationServiceRegistration — Scoped)
        // No AddScoped here; it is already registered in IntegrationServiceRegistration.

        // I-11 GOREV 3 — Zamanlanmis rapor uretimi job
        services.AddScoped<ScheduledReportGenerationJob>();

        // Billing — Abonelik yenileme ve dunning job'lari
        services.AddScoped<SubscriptionRenewalWorker>();
        services.AddScoped<DunningWorker>();

        // CRM — Loyalty points expiration worker
        services.AddScoped<ExpirePointsWorker>();

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
            job => job.CheckOverdueLeadsAsync(CancellationToken.None),
            "0 2 * * *");

        // Her gece 03:00 — görev süresi kontrolü
        RecurringJob.AddOrUpdate<CrmHangfireJobs>(
            "crm-overdue-tasks",
            job => job.CheckOverdueTasksAsync(CancellationToken.None),
            "0 3 * * *");

        // === MUH-01 DEV 4 — Muhasebe Accounting Workers ===

        // Her gun 03:30 — platform settlement sync
        RecurringJob.AddOrUpdate<SettlementSyncWorker>(
            "accounting-settlement-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "30 3 * * *");

        // Her 15 dakika — komisyon hesaplama
        RecurringJob.AddOrUpdate<CommissionCalculatorWorker>(
            "accounting-commission-calculator",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/15 * * * *");

        // Her gun 04:00 — banka ekstre import
        RecurringJob.AddOrUpdate<BankStatementImportWorker>(
            "accounting-bank-statement-import",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 4 * * *");

        // Her gun 23:59 — gunluk kar/zarar raporu
        RecurringJob.AddOrUpdate<DailyProfitWorker>(
            "accounting-daily-profit",
            job => job.ExecuteAsync(CancellationToken.None),
            "59 23 * * *");

        // === MUH-03 DEV 4 — E-posta Tarama Worker ===

        // Her 2 saatte bir — muhasebe e-posta tarama
        RecurringJob.AddOrUpdate<EmailScanWorker>(
            "accounting-email-scan",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 */2 * * *");

        // === MUH-02 DEV 4 — Ek Muhasebe Workers ===

        // Her 4 saatte bir — otomatik mutabakat eslestirme
        RecurringJob.AddOrUpdate<ReconciliationWorker>(
            "accounting-reconciliation",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 */4 * * *");

        // Her gun 08:00 — gunluk finansal brifing (WhatsApp/Telegram)
        RecurringJob.AddOrUpdate<ScheduledBriefingWorker>(
            "accounting-scheduled-briefing",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 8 * * *");

        // === MUH-03 DEV 6 — Aylik KDV taslagi ===

        // Her ayin 1'i saat 06:00 — onceki ay KDV taslagi
        RecurringJob.AddOrUpdate<TaxPrepWorker>(
            "accounting-tax-prep",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 6 1 * *");

        // === G-01 — Webhook Retry ===

        // Her dakika — basarisiz webhook'lari tekrar dene (1m, 5m, 30m artan aralik)
        RecurringJob.AddOrUpdate<WebhookRetryJob>(
            "webhook-retry",
            job => job.ExecuteAsync(CancellationToken.None),
            "* * * * *");

        // === Dalga 15 — ERP Sync Job'lari ===

        // Her 15 dakika — ERP stok seviyesi sync
        RecurringJob.AddOrUpdate<ErpStockSyncJob>(
            "erp-stock-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/15 * * * *");

        // Gunluk 4x (06:00, 12:00, 18:00, 23:00) — ERP fiyat sync
        RecurringJob.AddOrUpdate<ErpPriceSyncJob>(
            "erp-price-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 6,12,18,23 * * *");

        // Her gece 03:00 — ERP hesap/musteri sync
        RecurringJob.AddOrUpdate<ErpAccountSyncJob>(
            "erp-account-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(3));

        // Her 15 dakika — Parasut fatura sync (feature flag: Parasut.InvoiceSyncEnabled)
        RecurringJob.AddOrUpdate<ParasutInvoiceSyncJob>(
            "parasut-invoice-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/15 * * * *");

        // === I-11 GOREV 3 — Zamanlanmis Rapor Uretimi ===

        // Her gun 06:00 — gunluk satis raporu
        RecurringJob.AddOrUpdate<ScheduledReportGenerationJob>(
            "scheduled-report-daily-sales",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(6));

        // Her Pazartesi 08:00 — haftalik performans raporu
        RecurringJob.AddOrUpdate<ScheduledReportGenerationJob>(
            "scheduled-report-weekly-performance",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 8 * * 1");

        // Her ayin 1'i 06:00 — aylik finansal rapor
        RecurringJob.AddOrUpdate<ScheduledReportGenerationJob>(
            "scheduled-report-monthly-financial",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 6 1 * *");

        // === Dalga 10 — Sosyal Ticaret Feed Yenileme ===

        // Her 6 saatte bir — aktif SocialFeedConfiguration'lar icin feed uretimi
        SocialFeedRefreshJob.Register();

        // === CRM — Loyalty Points Expiration ===

        // Her gece 02:00 — 12 aydan eski kazanilmis puanlari surdur
        RecurringJob.AddOrUpdate<ExpirePointsWorker>(
            "crm-expire-loyalty-points",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(2));

        // === Billing — Abonelik Yenileme & Dunning ===

        // Her gun 03:00 — vadesi gelen abonelikleri otomatik yenile
        RecurringJob.AddOrUpdate<SubscriptionRenewalWorker>(
            "subscription-renewal",
            x => x.ProcessRenewalsAsync(CancellationToken.None),
            "0 3 * * *");

        // Her gun 04:00 — PastDue aboneliklere kademeli tahsilat escalation
        RecurringJob.AddOrUpdate<DunningWorker>(
            "dunning-escalation",
            x => x.ProcessDunningAsync(CancellationToken.None),
            "0 4 * * *");

        // ── Zincir 11: Gecikmiş sipariş kontrolü (her saat başı) ──
        RecurringJob.AddOrUpdate<CheckStaleOrdersJob>(
            "check-stale-orders",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 * * * *");

        // ── 14-Platform Stok Sync (GenericPlatformStockSyncJob) ──
        // Platform code = AdapterFactory key (PlatformCode property). Case-insensitive resolve.
        var platformSyncSchedules = new (string code, string cron)[]
        {
            ("Hepsiburada",  "*/30 * * * *"),  // 30dk — yüksek hacim
            ("Ciceksepeti",  "*/30 * * * *"),
            ("N11",          "*/30 * * * *"),
            ("Pazarama",     "*/30 * * * *"),
            ("Amazon",       "0 * * * *"),     // 1 saat — G494 FIX: Amazon_TR → Amazon (PlatformCode)
            ("AmazonEu",     "0 * * * *"),     // 1 saat — G495 FIX: eksikti, eklendi
            ("eBay",         "0 * * * *"),
            ("Shopify",      "0 * * * *"),
            ("WooCommerce",  "0 * * * *"),
            ("Ozon",         "0 */2 * * *"),   // 2 saat — düşük hacim
            ("Etsy",         "0 */2 * * *"),
            ("Zalando",      "0 */2 * * *"),
            ("PttAVM",       "0 */2 * * *"),
            // OpenCart → ayrı OpenCartStockSyncJob (self-hosted, özel mantık)
            // Bitrix24 → CRM adapter, stok sync uygulanamaz
        };

        foreach (var (code, cron) in platformSyncSchedules)
        {
            RecurringJob.AddOrUpdate<GenericPlatformStockSyncJob>(
                $"stock-sync-{code.ToLowerInvariant()}",
                job => job.ExecuteAsync(code, CancellationToken.None),
                cron);
        }

        // Her 30 dakika — Fulfillment stok sync (Amazon FBA + Hepsilojistik)
        RecurringJob.AddOrUpdate<FulfillmentStockSyncJob>(
            "fulfillment-stock-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/30 * * * *");
    }
}
