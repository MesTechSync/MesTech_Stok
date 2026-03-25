using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Infrastructure.AI;
using MesTech.Infrastructure.AI.Accounting;
using MesTech.Infrastructure.Auth;
using MesTech.Infrastructure.Banking;
using MesTech.Infrastructure.Finance;
using MesTech.Infrastructure.Banking.Parsers;
using MesTech.Infrastructure.Caching;
using MesTech.Infrastructure.Email;
using MesTech.Infrastructure.HealthChecks;
using MesTech.Infrastructure.Integration.Crm;
using MesTech.Infrastructure.Jobs;
using MesTech.Infrastructure.Jobs.Accounting;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Messaging.Mesa.Accounting;
using MesTech.Infrastructure.Persistence;
using MesTech.Infrastructure.Persistence.Repositories;
using Crm = MesTech.Infrastructure.Persistence.Repositories.Crm;
using Tasks = MesTech.Infrastructure.Persistence.Repositories.Tasks;
using Cal = MesTech.Infrastructure.Persistence.Repositories.Calendar;
using FinanceRepo = MesTech.Infrastructure.Persistence.Repositories.Finance;
using Hr = MesTech.Infrastructure.Persistence.Repositories.Hr;
using Docs = MesTech.Infrastructure.Persistence.Repositories.Documents;
using AccountingRepo = MesTech.Infrastructure.Persistence.Accounting.Repositories;
using MesTech.Infrastructure.Realtime;
using MesTech.Infrastructure.Security;
using MesTech.Infrastructure.Services;
using MesTech.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minio;

namespace MesTech.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool skipSelfHostedEndpoints = false)
    {
        // Tenant & User — Development ortami
        services.AddSingleton<ITenantProvider, DevelopmentTenantProvider>();
        services.AddSingleton<ICurrentUserService, DevelopmentUserService>();

        // Security — brute-force koruması + session + audit (İ-02)
        services.AddSingleton<LoginAttemptTracker>();
        services.AddSingleton<DesktopSessionManager>();
        services.AddSingleton<LoginAuditLogger>();

        // E-Fatura — XAdES + QuestPDF + Paraşüt sync (İ-08)
        services.Configure<Services.XAdESOptions>(configuration.GetSection("XAdES"));
        services.AddScoped<IDigitalSignatureService, Services.XAdESSignatureService>();
        services.AddScoped<IInvoicePdfGenerator, Services.QuestPdfInvoiceGenerator>();
        services.Configure<Integration.ERP.Parasut.ParasutOptions>(configuration.GetSection("Parasut"));
        services.AddScoped<Integration.ERP.IParasutInvoiceSyncService, Integration.ERP.ParasutInvoiceSyncService>();

        // AuditInterceptor
        services.AddScoped<AuditInterceptor>();

        // TenantContextInterceptor — PostgreSQL RLS tenant context (MUH-03)
        services.AddScoped<TenantContextInterceptor>();

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
            options.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<TenantContextInterceptor>());
        });

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IStoreCredentialRepository, StoreCredentialRepository>();
        services.AddScoped<IKvkkAuditLogRepository, KvkkAuditLogRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IIncomeRepository, IncomeRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICariHesapRepository, CariHesapRepository>();
        services.AddScoped<ICariHareketRepository, CariHareketRepository>();
        services.AddScoped<IProductSetRepository, ProductSetRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IBarcodeScanLogRepository, BarcodeScanLogRepository>();
        services.AddScoped<ILogEntryRepository, LogEntryRepository>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPriceRecommendationRepository, PriceRecommendationRepository>();
        services.AddScoped<IStockPredictionRepository, StockPredictionRepository>();
        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<IEInvoiceDocumentRepository, EInvoiceDocumentRepository>();
        services.AddScoped<IBitrix24DealRepository, Bitrix24DealRepository>();
        services.AddScoped<IBitrix24ContactRepository, Bitrix24ContactRepository>();
        services.AddScoped<ISyncLogRepository, SyncLogRepository>();
        services.AddScoped<IDashboardSummaryRepository, DashboardSummaryRepository>();

        // CRM Repositories (Dalga 8)
        services.AddScoped<ICrmLeadRepository, Crm.CrmLeadRepository>();
        services.AddScoped<ICrmContactRepository, Crm.CrmContactRepository>();
        services.AddScoped<ICrmDealRepository, Crm.CrmDealRepository>();

        // HR + Document + Pipeline Repositories (Dalga 8 H28)
        services.AddScoped<IDepartmentRepository, Hr.DepartmentRepository>();
        services.AddScoped<IEmployeeRepository, Hr.EmployeeRepository>();
        services.AddScoped<ILeaveRepository, Hr.LeaveRepository>();
        services.AddScoped<IDocumentFolderRepository, Docs.DocumentFolderRepository>();
        services.AddScoped<IDocumentRepository, Docs.DocumentRepository>();
        services.AddScoped<IPipelineRepository, Crm.PipelineRepository>();

        // Task / Calendar Repositories (Dalga 8 H27)
        services.AddScoped<IProjectRepository, Tasks.ProjectRepository>();
        services.AddScoped<IWorkTaskRepository, Tasks.WorkTaskRepository>();
        services.AddScoped<ICalendarEventRepository, Cal.CalendarEventRepository>();
        services.AddScoped<IFinanceExpenseRepository, FinanceRepo.ExpenseRepository>();

        // Finance — Kasa (DEV6)
        services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();

        // Billing — Abonelik (DEV6)
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ITenantSubscriptionRepository, TenantSubscriptionRepository>();
        services.AddScoped<IBillingInvoiceRepository, BillingInvoiceRepository>();
        services.AddScoped<IDunningLogRepository, DunningLogRepository>();

        // Onboarding (DEV6)
        services.AddScoped<IOnboardingProgressRepository, OnboardingProgressRepository>();

        // Dropshipping Pool Repository (Sprint-B)
        services.AddScoped<IDropshippingPoolRepository, DropshippingPoolRepository>();

        // Dropshipping Domain Repositories (Dalga 4)
        services.AddScoped<MesTech.Application.Interfaces.Dropshipping.IDropshipProductRepository,
            MesTech.Infrastructure.Persistence.Repositories.Dropshipping.DropshipProductRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Dropshipping.IDropshipOrderRepository,
            MesTech.Infrastructure.Persistence.Repositories.Dropshipping.DropshipOrderRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Dropshipping.IDropshipSupplierRepository,
            MesTech.Infrastructure.Persistence.Repositories.Dropshipping.DropshipSupplierRepository>();

        // Dropshipping Feed Repositories + Services (Sprint-D — Dalga 8)
        services.AddScoped<MesTech.Domain.Interfaces.ISupplierFeedRepository, SupplierFeedRepository>();
        services.AddScoped<IFeedImportLogRepository, FeedImportLogRepository>();
        services.AddScoped<IFeedSyncJobService, MesTech.Infrastructure.Jobs.FeedSyncJobService>();
        services.AddScoped<IFeedReliabilityScoreService, MesTech.Infrastructure.Services.FeedReliabilityScoreServiceAdapter>();

        // Dropship Feed Fetcher (K1d-05 — SyncDropshipProducts handler icin)
        services.AddScoped<MesTech.Application.Interfaces.Dropshipping.IDropshipFeedFetcher,
            MesTech.Infrastructure.Integration.Dropshipping.HttpDropshipFeedFetcher>();

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain Events
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Domain Services
        services.AddSingleton<StockCalculationService>();
        services.AddSingleton<PricingService>();
        services.AddSingleton<BarcodeValidationService>();
        services.AddSingleton<MesTech.Infrastructure.Security.BruteForceProtectionService>();
        services.AddScoped<MesTech.Domain.Services.IAutoShipmentService, MesTech.Domain.Services.AutoShipmentService>();

        // Reporting Repositories
        services.AddScoped<MesTech.Application.Interfaces.ISavedReportRepository,
            MesTech.Infrastructure.Persistence.Repositories.SavedReportRepository>();

        // Notification + ERP Repositories
        services.AddScoped<MesTech.Application.Interfaces.INotificationLogRepository,
            MesTech.Infrastructure.Persistence.Repositories.NotificationLogRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IUserNotificationRepository,
            MesTech.Infrastructure.Persistence.Repositories.UserNotificationRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Erp.IErpSyncLogRepository,
            MesTech.Infrastructure.Persistence.Repositories.Erp.ErpSyncLogRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.INotificationSettingRepository,
            MesTech.Infrastructure.Persistence.Repositories.NotificationSettingRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IPlatformMessageRepository,
            MesTech.Infrastructure.Persistence.Repositories.Crm.PlatformMessageRepository>();

        services.AddScoped<MesTech.Application.Interfaces.IBulkProductImportService,
            MesTech.Infrastructure.Services.BulkProductImportService>();
        services.AddScoped<MesTech.Application.Interfaces.ICategoryPlatformMappingRepository,
            MesTech.Infrastructure.Persistence.Repositories.CategoryPlatformMappingRepository>();
        services.AddScoped<MesTech.Application.Interfaces.ICrmDashboardQueryService,
            MesTech.Infrastructure.Services.CrmDashboardQueryService>();
        // ICargoRateProvider: 7 kargo adaptor dogrudan implement eder.
        // CargoProviderSelector ve GetCargoComparisonHandler cast pattern ile erisir:
        //   adapter is ICargoRateProvider rateProvider
        // Standalone DI registration gereksiz — factory + cast yeterli.

        // Message Publisher (MassTransit wrapper)
        services.AddScoped<MesTech.Application.Interfaces.IMessagePublisher,
            MesTech.Infrastructure.Messaging.MassTransitMessagePublisher>();

        // Encryption
        var encryptionKey = configuration["Security:EncryptionKey"]
            ?? AesGcmEncryptionService.GenerateKey();
        services.AddSingleton(new AesGcmEncryptionService(encryptionKey));

        // JWT Token Service (Dalga 9 — Blazor SaaS authentication)
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // === FAZ 1: ALTYAPI AKTIFLESTIRME ===

        // Redis Cache
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:Configuration"]
            ?? "localhost:3679";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "MesTech_";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Distributed Lock — Redis if available, InProcess fallback for dev/test
        var redisConfig = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:Configuration"];
        if (!string.IsNullOrEmpty(redisConfig))
        {
            services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        }
        else
        {
            services.AddSingleton<IDistributedLockService, InProcessDistributedLockService>();
        }

        // Exchange Rate Service — TCMB XML API, IMemoryCache 1h TTL (Dalga 11 — Multi-currency)
        services.AddMemoryCache();
        services.AddHttpClient<IExchangeRateService, ExchangeRateService>();

        // RabbitMQ MassTransit Event Bus
        services.AddMesTechMessaging(configuration);
        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();

        // === MESA OS Bridge (Dalga 1: Mock, Dalga 8 H28: Real HTTP via feature flag) ===
        // Feature flag: Mesa:UseProductionBridge=true → ProductionMesaAIService (HTTP REST)
        //               Mesa:UseProductionBridge=false (default) → MockMesaAIService
        var useMesaProd = configuration.GetValue<bool>("Mesa:UseProductionBridge", false);
        // MockMesaAIService is always registered — ProductionMesaAIService uses it as a fallback
        services.AddScoped<MockMesaAIService>();
        if (useMesaProd)
            services.AddHttpClient<IMesaAIService, ProductionMesaAIService>();
        else
            services.AddScoped<IMesaAIService, MockMesaAIService>();
        if (useMesaProd)
            services.AddHttpClient<IMesaBotService, ProductionMesaBotService>();
        else
            services.AddScoped<IMesaBotService, MockMesaBotService>();

        // Feature flag: Mesa:BridgeEnabled=true → RealMesaEventPublisher (HTTP REST)
        //               Mesa:BridgeEnabled=false (default) → MesaEventPublisher (MassTransit/Mock)
        var mesaBridgeEnabled = configuration.GetValue<bool>("Mesa:BridgeEnabled", false);
        if (mesaBridgeEnabled)
            services.AddHttpClient<IMesaEventPublisher, RealMesaEventPublisher>();
        else
            services.AddScoped<IMesaEventPublisher, MesaEventPublisher>();

        services.AddSingleton<IMesaEventMonitor, MesaEventMonitor>();

        // Dalga 4: AI servisleri (Mock — Dalga 5'te Real swap)
        services.AddScoped<IBuyboxService, MockBuyboxService>();
        services.AddScoped<IStockPredictionService, MockStockPredictionService>();
        services.AddScoped<IPriceOptimizationService, MockPriceOptimizationService>();
        services.AddScoped<IProductSearchService, MockProductSearchService>();

        // MESA Status Endpoint (http://localhost:3101/api/mesa/status)
        if (!skipSelfHostedEndpoints)
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
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<AhmetBeyDemoSeeder>();

        // HealthCheck
        services.AddSingleton<PlatformHealthCheckService>();
        services.AddHttpClient("MesaOSHealth");
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis")
            .AddCheck<PostgresHealthCheck>("postgresql")
            .AddCheck<MesaOSHealthCheck>("mesa-os")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq")
            .AddCheck<MinioHealthCheck>("minio");

        // Health Check HTTP Endpoint (http://localhost:3100/health)
        if (!skipSelfHostedEndpoints)
            services.AddHostedService(sp =>
                new HealthCheckEndpoint(
                    sp.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HealthCheckEndpoint>>()));

        // ── Realtime Dashboard (port 3102) ──
        services.AddSingleton<WebSocketConnectionManager>();
        services.AddSingleton<IDashboardNotifier, WebSocketDashboardNotifier>();
        if (!skipSelfHostedEndpoints)
            services.AddHostedService(sp => new RealtimeDashboardEndpoint(
                sp.GetRequiredService<WebSocketConnectionManager>(),
                sp.GetRequiredService<ILogger<RealtimeDashboardEndpoint>>(),
                port: configuration.GetValue<int>("Realtime:WebSocketPort", 3102)
            ));

        // Email Sender (S05 — MailKit SMTP)
        services.Configure<Email.SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddTransient<IEmailService, Email.MailKitEmailService>();

        // Barcode Generation (S06f — Code128 + EAN-13)
        services.AddSingleton<IBarcodeGenerationService, BarcodeGenerationService>();

        // XML Import / Export
        services.AddScoped<IXmlImportService, XmlImportService>();
        services.AddScoped<IXmlExportService, XmlExportService>();

        // Excel Import / Export
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();

        // Generic Report Export (Excel + CSV) — G-03e
        services.AddScoped<IReportExportService, ReportExportService>();

        // CRM-Order Kopru Servisi (Dalga 8 H27)
        services.AddScoped<ICrmOrderBridgeService, CrmOrderBridgeService>();

        // Document Storage (Dalga 8 H27 — MinIO SDK gercek implementasyon)
        services.AddSingleton<IMinioClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var endpoint = config["MinIO:Endpoint"] ?? "localhost:3900";
            var accessKey = config["MinIO:AccessKey"]
                ?? throw new InvalidOperationException("MinIO:AccessKey config required. Set in appsettings.json or env.");
            var secretKey = config["MinIO:SecretKey"]
                ?? throw new InvalidOperationException("MinIO:SecretKey config required. Set in appsettings.json or env.");
            return new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();
        });
        services.AddScoped<IDocumentStorageService, MinioDocumentStorageService>();

        // === Label Print Service (barcode/product label generation) ===
        services.AddScoped<ILabelPrintService, LabelPrintService>();

        // === Sandbox Test Runner (G-05a: adapter connectivity testing) ===
        services.AddScoped<ISandboxTestRunner, SandboxTestRunner>();

        // === Webhook Receiver + SignalR Real-Time (G-01 + G-02) ===
        services.AddWebhookServices();

        // === Integration Layer (Adapters, Factory, Orchestrator) ===
        services.AddIntegrationServices();

        // === Muhasebe Modulu (MUH-01 + MUH-02) ===
        services.AddAccountingServices(configuration);

        return services;
    }

    /// <summary>
    /// Muhasebe modulu servis ve repository kayitlari.
    /// MUH-02: Feature flag swap + Advisory + Anomaly handler.
    /// </summary>
    public static IServiceCollection AddAccountingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Domain Services ──
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ICommissionRateProvider,
            MesTech.Infrastructure.Integration.Accounting.PlatformCommissionRateProvider>();
        services.AddScoped<MesTech.Domain.Accounting.Services.ICommissionCalculationService>(sp =>
        {
            var rateProvider = sp.GetRequiredService<MesTech.Application.Interfaces.Accounting.ICommissionRateProvider>();
            return new MesTech.Domain.Accounting.Services.CommissionCalculationService(
                async (platform, category, ct) =>
                {
                    var info = await rateProvider.GetRateAsync(platform, category, ct);
                    if (info is null) return null;
                    return new MesTech.Domain.Accounting.Services.DynamicRateResult(
                        info.Rate, info.Source, info.CachedUntil);
                });
        });
        services.AddSingleton<MesTech.Domain.Accounting.Services.ITaxWithholdingService,
            MesTech.Domain.Accounting.Services.TaxWithholdingService>();
        services.AddSingleton<MesTech.Domain.Accounting.Services.IProfitCalculationService,
            MesTech.Domain.Accounting.Services.ProfitCalculationService>();
        services.AddSingleton<MesTech.Domain.Accounting.Services.IReconciliationScoringService,
            MesTech.Domain.Accounting.Services.ReconciliationScoringService>();

        // ── MESA Muhasebe AI (MUH-02: Feature flag swap — Mock veya Real) ──
        // MockMesaAccountingService her zaman kayitli: Real client fallback olarak kullanir
        services.AddSingleton<MockMesaAccountingService>();

        if (configuration.GetValue<bool>("Mesa:Accounting:UseReal", false))
        {
            services.AddHttpClient<MesTech.Application.Interfaces.Accounting.IMesaAccountingService,
                RealMesaAccountingClient>();
        }
        else
        {
            services.AddScoped<MesTech.Application.Interfaces.Accounting.IMesaAccountingService,
                MockMesaAccountingService>();
        }

        // ── MESA Advisory Agent (MUH-02: Feature flag swap) ──
        if (configuration.GetValue<bool>("Mesa:Advisory:UseReal", false))
        {
            services.AddHttpClient<IAdvisoryAgentClient, AdvisoryAgentClient>();
        }
        else
        {
            services.AddScoped<IAdvisoryAgentClient, MockAdvisoryAgentClient>();
        }

        // ── MESA Advisory Agent V2 — "Bugun ne sat" (MUH-03: Feature flag swap) ──
        if (configuration.GetValue<bool>("Mesa:Advisory:UseReal", false))
        {
            services.AddHttpClient<IAdvisoryAgentV2, AdvisoryAgentV2>();
        }
        else
        {
            services.AddScoped<IAdvisoryAgentV2, MockAdvisoryAgentV2>();
        }

        // ── TaxPrep Agent (MUH-03) ──
        services.AddScoped<ITaxPrepAgent, TaxPrepAgent>();

        // ── Repositories ──
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IChartOfAccountsRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.ChartOfAccountsRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ICounterpartyRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.CounterpartyRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IJournalEntryRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.JournalEntryRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ISettlementBatchRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.SettlementBatchRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IBankTransactionRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.BankTransactionRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IReconciliationMatchRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.ReconciliationMatchRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IAccountingDocumentRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.AccountingDocumentRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ICashFlowEntryRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.CashFlowEntryRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IExpenseCategoryRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.ExpenseCategoryRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ICommissionRecordRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.CommissionRecordRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ICargoExpenseRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.CargoExpenseRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IPersonalExpenseRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.PersonalExpenseRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ITaxRecordRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.TaxRecordRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ITaxWithholdingRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.TaxWithholdingRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IFinancialGoalRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.FinancialGoalRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IProfitReportRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.ProfitReportRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IAccountingSupplierAccountRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.AccountingSupplierAccountRepository>();

        // Dalga N1 — Muhasebe genisleme
        services.AddScoped<MesTech.Application.Interfaces.Accounting.ISalaryRecordRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.SalaryRecordRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IFixedExpenseRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.FixedExpenseRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IPenaltyRecordRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.PenaltyRecordRepository>();

        // ── Domain Services (somut sınıflar — handler'lar doğrudan enjekte eder) ──
        services.AddScoped<MesTech.Domain.Accounting.Services.BalanceSheetValidationService>();
        services.AddScoped<MesTech.Domain.Accounting.Services.TrialBalanceValidationService>();

        // ── Application Services ──
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IFifoCostCalculationService,
            MesTech.Application.Services.Accounting.FifoCostCalculationService>();

        // ── PlatformCommission Repository ──
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IPlatformCommissionRepository,
            MesTech.Infrastructure.Persistence.Accounting.Repositories.PlatformCommissionRepository>();

        // ── FixedAsset + BaBs Repositories (DALGA 14 Muhasebe) ──
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IFixedAssetRepository,
            MesTech.Infrastructure.Persistence.Repositories.Accounting.FixedAssetRepository>();
        services.AddScoped<MesTech.Application.Interfaces.Accounting.IBaBsRecordRepository,
            MesTech.Infrastructure.Persistence.Repositories.Accounting.BaBsRecordRepository>();
        services.AddSingleton<MesTech.Domain.Accounting.Services.DepreciationCalculationService>();

        // ── ShipmentCost + AccountingPeriod Repositories (DEV 1 V4) ──
        services.AddScoped<MesTech.Domain.Interfaces.IShipmentCostRepository,
            MesTech.Infrastructure.Persistence.Repositories.ShipmentCostRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IAccountingPeriodRepository,
            MesTech.Infrastructure.Persistence.Repositories.AccountingPeriodRepository>();

        // ── StockSplit / Fulfillment Stock Service ──
        services.AddScoped<MesTech.Application.Interfaces.IStockSplitService,
            MesTech.Infrastructure.Services.StockSplitService>();

        // ── VUK WORM Document Store (MUH-03) ──
        services.AddScoped<IImmutableDocumentStore,
            MesTech.Infrastructure.Persistence.Accounting.ImmutableDocumentStore>();

        // ── Bank Statement Parsers (MUH-01 DEV 4) ──
        services.AddSingleton<IBankStatementParser, OFXParser>();
        services.AddSingleton<IBankStatementParser, MT940Parser>();
        services.AddSingleton<IBankStatementParser, Camt053Parser>();
        services.AddSingleton<IBankStatementParser, CsvStatementParser>();
        services.AddSingleton<IBankStatementParserFactory, BankStatementParserFactory>();

        // ── Bank Statement Import Service ──
        services.AddScoped<BankStatementImportService>();

        // ── Field Encryption Service ──
        services.AddSingleton<IFieldEncryptionService, FieldEncryptionService>();

        // ── Credential Encryption Service (StoreCredential sifreleme + maskeleme) ──
        services.AddSingleton<ICredentialEncryptionService, CredentialEncryptionService>();

        // ── Key Rotation Service (MUH-02 KVKK) ──
        services.AddSingleton<IKeyRotationService, KeyRotationService>();

        // ── Accounting Hangfire Workers ──
        services.AddScoped<SettlementSyncWorker>();
        services.AddScoped<CommissionCalculatorWorker>();
        services.AddScoped<BankStatementImportWorker>();
        services.AddScoped<DailyProfitWorker>();

        // ── MUH-02 Accounting Workers ──
        services.AddScoped<ReconciliationWorker>();
        services.AddScoped<ScheduledBriefingWorker>();

        // ── MUH-03 Accounting Workers ──
        services.AddScoped<TaxPrepWorker>();

        // ── MUH-03 DEV 4 — Ses Muhasebe + E-posta IMAP ──
        services.AddHttpClient<ISpeechToExpenseService, SpeechToExpenseService>();
        services.AddScoped<IAccountingEmailScanner, AccountingEmailScanner>();
        services.AddScoped<EmailScanWorker>();

        // ── L3e: Muhasebe Ek Servisler ──
        services.AddSingleton<IDepreciationService, MesTech.Infrastructure.Finance.DepreciationService>();
        services.AddSingleton<IIncomeTaxService, MesTech.Infrastructure.Finance.IncomeTaxService>();
        services.AddScoped<IBaBsReportService, MesTech.Infrastructure.Finance.BaBsReportService>();
        services.AddScoped<IBaBsXmlExportService, MesTech.Infrastructure.Finance.BaBsXmlExportService>();
        services.AddScoped<IBankReconciliationReportService, MesTech.Infrastructure.Finance.BankReconciliationReportService>();

        // ── MUH-02: Anomaly Check Handler (MediatR INotificationHandler) ──
        // Infrastructure assembly MediatR scan'e dahil degil, explicit kayit.
        services.AddScoped<INotificationHandler<DomainEventNotification<LedgerPostedEvent>>,
            AnomalyCheckHandler>();

        // ── Application EventHandler DI Registration ──
        // 52 Application handler interface+class tanımlı ama dispatch zincirine bağlı DEĞİLDİ.
        // Bridge handler'lar DomainEventNotification<T> → Application interface çağırır.
        // Kritik iş zincirleri:
        services.AddScoped<Application.EventHandlers.IOrderPlacedEventHandler,
            Application.EventHandlers.OrderPlacedStockDeductionHandler>();
        services.AddScoped<Application.EventHandlers.IOrderConfirmedRevenueHandler,
            Application.EventHandlers.OrderConfirmedRevenueHandler>();
        services.AddScoped<Application.EventHandlers.IReturnApprovedStockRestorationHandler,
            Application.EventHandlers.ReturnApprovedStockRestorationHandler>();
        services.AddScoped<Application.EventHandlers.IReturnJournalReversalHandler,
            Application.EventHandlers.ReturnJournalReversalHandler>();
        services.AddScoped<Application.EventHandlers.IShipmentCostRecordedEventHandler,
            Application.EventHandlers.ShipmentCostRecordedEventHandler>();
        services.AddScoped<Application.EventHandlers.IZeroStockEventHandler,
            Application.EventHandlers.ZeroStockDetectedEventHandler>();
        services.AddScoped<Application.EventHandlers.IPriceLossEventHandler,
            Application.EventHandlers.PriceLossDetectedEventHandler>();
        services.AddScoped<Application.EventHandlers.IStaleOrderEventHandler,
            Application.EventHandlers.StaleOrderDetectedEventHandler>();
        services.AddScoped<Application.EventHandlers.IOrderCancelledEventHandler,
            Application.EventHandlers.OrderCancelledEventHandler>();

        // Batch 2 — DIRECT event handlers (event'i doğrudan alan)
        services.AddScoped<Application.EventHandlers.IOrderReceivedEventHandler,
            Application.EventHandlers.OrderReceivedEventHandler>();
        services.AddScoped<Application.EventHandlers.IOrderShippedEventHandler,
            Application.EventHandlers.OrderShippedEventHandler>();
        services.AddScoped<Application.EventHandlers.IReturnCreatedEventHandler,
            Application.EventHandlers.ReturnCreatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IReturnResolvedEventHandler,
            Application.EventHandlers.ReturnResolvedEventHandler>();
        services.AddScoped<Application.EventHandlers.IProductCreatedEventHandler,
            Application.EventHandlers.ProductCreatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IProductUpdatedEventHandler,
            Application.EventHandlers.ProductUpdatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IPriceChangedEventHandler,
            Application.EventHandlers.PriceChangedEventHandler>();
        services.AddScoped<Application.EventHandlers.IStockCriticalEventHandler,
            Application.EventHandlers.StockCriticalEventHandler>();
        services.AddScoped<Application.EventHandlers.IBuyboxLostEventHandler,
            Application.EventHandlers.BuyboxLostEventHandler>();
        services.AddScoped<Application.EventHandlers.IInvoiceApprovedEventHandler,
            Application.EventHandlers.InvoiceApprovedEventHandler>();
        services.AddScoped<Application.EventHandlers.IInvoiceGeneratedForERPEventHandler,
            Application.EventHandlers.InvoiceGeneratedForERPEventHandler>();
        services.AddScoped<Application.EventHandlers.IBaBsRecordCreatedEventHandler,
            Application.EventHandlers.BaBsRecordCreatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IBankStatementImportedEventHandler,
            Application.EventHandlers.BankStatementImportedEventHandler>();
        services.AddScoped<Application.EventHandlers.ICashTransactionRecordedEventHandler,
            Application.EventHandlers.CashTransactionRecordedEventHandler>();
        services.AddScoped<Application.EventHandlers.IExpenseCreatedEventHandler,
            Application.EventHandlers.ExpenseCreatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IExpensePaidEventHandler,
            Application.EventHandlers.ExpensePaidEventHandler>();
        services.AddScoped<Application.EventHandlers.IFixedAssetCreatedEventHandler,
            Application.EventHandlers.FixedAssetCreatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IProfitReportGeneratedEventHandler,
            Application.EventHandlers.ProfitReportGeneratedEventHandler>();
        services.AddScoped<Application.EventHandlers.IReconciliationCompletedEventHandler,
            Application.EventHandlers.ReconciliationCompletedEventHandler>();
        services.AddScoped<Application.EventHandlers.IReconciliationMatchedEventHandler,
            Application.EventHandlers.ReconciliationMatchedEventHandler>();
        services.AddScoped<Application.EventHandlers.ITaxWithholdingComputedEventHandler,
            Application.EventHandlers.TaxWithholdingComputedEventHandler>();
        services.AddScoped<Application.EventHandlers.IDailySummaryGeneratedEventHandler,
            Application.EventHandlers.DailySummaryGeneratedEventHandler>();
        services.AddScoped<Application.EventHandlers.ISyncRequestedEventHandler,
            Application.EventHandlers.SyncRequestedEventHandler>();
        services.AddScoped<Application.EventHandlers.ISyncErrorOccurredEventHandler,
            Application.EventHandlers.SyncErrorOccurredEventHandler>();
        services.AddScoped<Application.EventHandlers.ISupplierFeedSyncedEventHandler,
            Application.EventHandlers.SupplierFeedSyncedEventHandler>();
        services.AddScoped<Application.EventHandlers.IPlatformNotificationFailedEventHandler,
            Application.EventHandlers.PlatformNotificationFailedEventHandler>();
        services.AddScoped<Application.EventHandlers.IDealLostEventHandler,
            Application.EventHandlers.DealLostEventHandler>();
        services.AddScoped<Application.EventHandlers.IDealStageChangedEventHandler,
            Application.EventHandlers.DealStageChangedEventHandler>();
        services.AddScoped<Application.EventHandlers.IDealWonEventHandler,
            Application.EventHandlers.DealWonEventHandler>();
        services.AddScoped<Application.EventHandlers.ILeadConvertedEventHandler,
            Application.EventHandlers.LeadConvertedEventHandler>();
        services.AddScoped<Application.EventHandlers.ILeaveApprovedEventHandler,
            Application.EventHandlers.LeaveApprovedEventHandler>();
        services.AddScoped<Application.EventHandlers.IDocumentUploadedEventHandler,
            Application.EventHandlers.DocumentUploadedEventHandler>();
        services.AddScoped<Application.EventHandlers.ICalendarEventCreatedEventHandler,
            Application.EventHandlers.CalendarEventCreatedEventHandler>();
        services.AddScoped<Application.EventHandlers.ITaskCompletedEventHandler,
            Application.EventHandlers.TaskCompletedEventHandler>();
        services.AddScoped<Application.EventHandlers.ITaskOverdueEventHandler,
            Application.EventHandlers.TaskOverdueEventHandler>();

        // Batch 3 — PARAMS event handlers (event property'lerinden parametre çekilen)
        services.AddScoped<Application.EventHandlers.IEInvoiceCancelledEventHandler,
            Application.EventHandlers.EInvoiceCancelledEventHandler>();
        services.AddScoped<Application.EventHandlers.IEInvoiceCreatedEventHandler,
            Application.EventHandlers.EInvoiceCreatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IEInvoiceSentEventHandler,
            Application.EventHandlers.EInvoiceSentEventHandler>();
        services.AddScoped<Application.EventHandlers.IExpenseApprovedEventHandler,
            Application.EventHandlers.ExpenseApprovedEventHandler>();
        services.AddScoped<Application.EventHandlers.ILeaveRejectedEventHandler,
            Application.EventHandlers.LeaveRejectedEventHandler>();
        services.AddScoped<Application.EventHandlers.INotificationSettingsUpdatedEventHandler,
            Application.EventHandlers.NotificationSettingsUpdatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IOnboardingCompletedEventHandler,
            Application.EventHandlers.OnboardingCompletedEventHandler>();
        services.AddScoped<Application.EventHandlers.IPaymentFailedEventHandler,
            Application.EventHandlers.PaymentFailedEventHandler>();
        services.AddScoped<Application.EventHandlers.ISubscriptionCancelledEventHandler,
            Application.EventHandlers.SubscriptionCancelledEventHandler>();
        services.AddScoped<Application.EventHandlers.ISubscriptionCreatedEventHandler,
            Application.EventHandlers.SubscriptionCreatedEventHandler>();
        services.AddScoped<Application.EventHandlers.IPlatformMessageReceivedEventHandler,
            Application.EventHandlers.PlatformMessageReceivedEventHandler>();

        // Batch 4 — Muhasebe GL handler'lar (repo-enriched bridges)
        services.AddScoped<Application.EventHandlers.IInvoiceApprovedGLHandler,
            Application.EventHandlers.InvoiceApprovedGLHandler>();
        services.AddScoped<Application.EventHandlers.IInvoiceCancelledReversalHandler,
            Application.EventHandlers.InvoiceCancelledReversalHandler>();
        services.AddScoped<Application.EventHandlers.IOrderShippedCostHandler,
            Application.EventHandlers.OrderShippedCostHandler>();
        services.AddScoped<Application.EventHandlers.ICommissionChargedGLHandler,
            Application.EventHandlers.CommissionChargedGLHandler>();

        return services;
    }
}
