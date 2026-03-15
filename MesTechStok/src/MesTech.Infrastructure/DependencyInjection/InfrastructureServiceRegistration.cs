using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Infrastructure.AI;
using MesTech.Infrastructure.AI.Accounting;
using MesTech.Infrastructure.Banking;
using MesTech.Infrastructure.Banking.Parsers;
using MesTech.Infrastructure.Caching;
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
using Finance = MesTech.Infrastructure.Persistence.Repositories.Finance;
using Hr = MesTech.Infrastructure.Persistence.Repositories.Hr;
using Docs = MesTech.Infrastructure.Persistence.Repositories.Documents;
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
        services.AddScoped<IIncomeRepository, IncomeRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICariHesapRepository, CariHesapRepository>();
        services.AddScoped<ICariHareketRepository, CariHareketRepository>();
        services.AddScoped<IProductSetRepository, ProductSetRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IBarcodeScanLogRepository, BarcodeScanLogRepository>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IBitrix24DealRepository, Bitrix24DealRepository>();
        services.AddScoped<IBitrix24ContactRepository, Bitrix24ContactRepository>();

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
        services.AddScoped<IFinanceExpenseRepository, Finance.ExpenseRepository>();

        // Dropshipping Pool Repository (Sprint-B)
        services.AddScoped<IDropshippingPoolRepository, DropshippingPoolRepository>();

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

        // === MESA OS Bridge (Dalga 1: Mock, Dalga 8 H28: Real HTTP via feature flag) ===
        services.AddScoped<IMesaAIService, MockMesaAIService>();
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

        // ── Realtime Dashboard (port 5102) ──
        services.AddSingleton<WebSocketConnectionManager>();
        services.AddSingleton<IDashboardNotifier, WebSocketDashboardNotifier>();
        services.AddHostedService(sp => new RealtimeDashboardEndpoint(
            sp.GetRequiredService<WebSocketConnectionManager>(),
            sp.GetRequiredService<ILogger<RealtimeDashboardEndpoint>>(),
            port: configuration.GetValue<int>("Realtime:WebSocketPort", 5102)
        ));

        // XML Import / Export
        services.AddScoped<IXmlImportService, XmlImportService>();
        services.AddScoped<IXmlExportService, XmlExportService>();

        // Excel Import / Export
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();

        // CRM-Order Kopru Servisi (Dalga 8 H27)
        services.AddScoped<ICrmOrderBridgeService, CrmOrderBridgeService>();

        // Document Storage (Dalga 8 H27 — MinIO SDK gercek implementasyon)
        services.AddSingleton<IMinioClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new MinioClient()
                .WithEndpoint(config["MinIO:Endpoint"] ?? "localhost:9000")
                .WithCredentials(
                    config["MinIO:AccessKey"] ?? "mestech_minio",
                    config["MinIO:SecretKey"] ?? "changeme")
                .Build();
        });
        services.AddScoped<IDocumentStorageService, MinioDocumentStorageService>();

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
        services.AddSingleton<MesTech.Domain.Accounting.Services.ICommissionCalculationService,
            MesTech.Domain.Accounting.Services.CommissionCalculationService>();
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

        // ── Bank Statement Parsers (MUH-01 DEV 4) ──
        services.AddSingleton<IBankStatementParser, OFXParser>();
        services.AddSingleton<IBankStatementParser, MT940Parser>();
        services.AddSingleton<IBankStatementParser, Camt053Parser>();
        services.AddSingleton<IBankStatementParserFactory, BankStatementParserFactory>();

        // ── Bank Statement Import Service ──
        services.AddScoped<BankStatementImportService>();

        // ── Field Encryption Service ──
        services.AddSingleton<IFieldEncryptionService, FieldEncryptionService>();

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

        // ── MUH-02: Anomaly Check Handler (MediatR INotificationHandler) ──
        // Infrastructure assembly MediatR scan'e dahil degil, explicit kayit.
        services.AddScoped<INotificationHandler<DomainEventNotification<LedgerPostedEvent>>,
            AnomalyCheckHandler>();

        return services;
    }
}
