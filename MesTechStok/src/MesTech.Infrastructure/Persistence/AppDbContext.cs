using System.Linq.Expressions;
using MesTech.Domain.Common;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.AI;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext — PostgreSQL + Domain Entity'ler + Global Tenant Filter.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IFieldEncryptionService? _encryptionService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantProvider tenantProvider,
        IFieldEncryptionService encryptionService)
        : base(options)
    {
        _tenantProvider = tenantProvider;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// EF Core query filter'ın her sorguda dinamik olarak değerlendireceği property.
    /// Expression.Constant(value) kullanmak yerine bu property'yi yakala —
    /// EF Core, DbContext instance'ını query-time'da swap eder.
    /// </summary>
    internal Guid CurrentTenantId => _tenantProvider.GetCurrentTenantId();

    // ── Domain Entities ──
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseZone> WarehouseZones => Set<WarehouseZone>();
    public DbSet<WarehouseRack> WarehouseRacks => Set<WarehouseRack>();
    public DbSet<WarehouseShelf> WarehouseShelves => Set<WarehouseShelf>();
    public DbSet<WarehouseBin> WarehouseBins => Set<WarehouseBin>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();
    public DbSet<ApiCallLog> ApiCallLogs => Set<ApiCallLog>();
    public DbSet<BarcodeScanLog> BarcodeScanLogs => Set<BarcodeScanLog>();
    public DbSet<CircuitStateLog> CircuitStateLogs => Set<CircuitStateLog>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<OfflineQueueItem> OfflineQueueItems => Set<OfflineQueueItem>();
    public DbSet<SyncRetryItem> SyncRetryItems => Set<SyncRetryItem>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    // ── Yeni Entity'ler (DEV1 Emirname) ──
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreCredential> StoreCredentials => Set<StoreCredential>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductPlatformMapping> ProductPlatformMappings => Set<ProductPlatformMapping>();
    public DbSet<BrandPlatformMapping> BrandPlatformMappings => Set<BrandPlatformMapping>();
    public DbSet<CategoryPlatformMapping> CategoryPlatformMappings => Set<CategoryPlatformMapping>();

    // ── Fatura Entity'leri (Faz 1) ──
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    // ── Dalga 5: Fatura Sablon + Kontor ──
    public DbSet<InvoiceTemplate> InvoiceTemplates => Set<InvoiceTemplate>();
    public DbSet<KontorBalance> KontorBalances => Set<KontorBalance>();

    // ── Dalga 4: Finansal Entity'ler ──
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnRequestLine> ReturnRequestLines => Set<ReturnRequestLine>();
    public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
    public DbSet<SupplierAccount> SupplierAccounts => Set<SupplierAccount>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
    public DbSet<PlatformCommission> PlatformCommissions => Set<PlatformCommission>();
    public DbSet<PlatformPayment> PlatformPayments => Set<PlatformPayment>();
    public DbSet<ProductWarehouseStock> ProductWarehouseStocks => Set<ProductWarehouseStock>();

    // ── Dalga 4: Dropshipping ──
    public DbSet<DropshipSupplier> DropshipSuppliers => Set<DropshipSupplier>();
    public DbSet<DropshipProduct> DropshipProducts => Set<DropshipProduct>();
    public DbSet<DropshipOrder> DropshipOrders => Set<DropshipOrder>();
    public DbSet<SupplierFeed> SupplierFeeds => Set<SupplierFeed>();

    // ── Dalga 8: Dropshipping Pool ──
    public DbSet<DropshippingPool> DropshippingPools => Set<DropshippingPool>();
    public DbSet<DropshippingPoolProduct> DropshippingPoolProducts => Set<DropshippingPoolProduct>();
    public DbSet<FeedImportLog> FeedImportLogs => Set<FeedImportLog>();

    // ── Dalga 4: AI ──
    public DbSet<PriceRecommendation> PriceRecommendations => Set<PriceRecommendation>();
    public DbSet<StockPrediction> StockPredictions => Set<StockPrediction>();

    // ── Dalga 5: OnMuhasebe Entity'leri (A-05) ──
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CariHesap> CariHesaplar => Set<CariHesap>();
    public DbSet<CariHareket> CariHareketler => Set<CariHareket>();

    // ── G7 C-05: ProductSet (ürün seti) ──
    public DbSet<ProductSet> ProductSets => Set<ProductSet>();
    public DbSet<ProductSetItem> ProductSetItems => Set<ProductSetItem>();

    // ── Dalga 6: Teklif (Quotation) ──
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationLine> QuotationLines => Set<QuotationLine>();

    // ── Dalga 7: Bitrix24 CRM ──
    public DbSet<Bitrix24Deal> Bitrix24Deals => Set<Bitrix24Deal>();
    public DbSet<Bitrix24Contact> Bitrix24Contacts => Set<Bitrix24Contact>();
    public DbSet<Bitrix24DealProductRow> Bitrix24DealProductRows => Set<Bitrix24DealProductRow>();

    // ═══════════════════════════════════════
    // DALGA 8 — CRM
    // ═══════════════════════════════════════
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<CrmContact> CrmContacts => Set<CrmContact>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<PlatformMessage> PlatformMessages => Set<PlatformMessage>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignProduct> CampaignProducts => Set<CampaignProduct>();
    public DbSet<LoyaltyProgram> LoyaltyPrograms => Set<LoyaltyProgram>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    // ═══════════════════════════════════════
    // DALGA 8 — FİNANS
    // ═══════════════════════════════════════
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<GLTransaction> GLTransactions => Set<GLTransaction>();
    public DbSet<FinanceExpense> FinanceExpenses => Set<FinanceExpense>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();

    // ═══════════════════════════════════════
    // BILLING & ONBOARDING
    // ═══════════════════════════════════════
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<DunningLog> DunningLogs => Set<DunningLog>();
    public DbSet<OnboardingProgress> OnboardingProgress => Set<OnboardingProgress>();

    // ═══════════════════════════════════════
    // DALGA 8 — GÖREVLER & PROJELER
    // ═══════════════════════════════════════
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    // ═══════════════════════════════════════
    // DALGA 8 — TAKVİM
    // ═══════════════════════════════════════
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<CalendarEventAttendee> CalendarEventAttendees => Set<CalendarEventAttendee>();

    // ═══════════════════════════════════════
    // DALGA 8 — BELGELER
    // ═══════════════════════════════════════
    public DbSet<DocumentFolder> DocumentFolders => Set<DocumentFolder>();
    public DbSet<Document> Documents => Set<Document>();

    // ═══════════════════════════════════════
    // DALGA 8 — İNSAN KAYNAKLARI
    // ═══════════════════════════════════════
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Leave> Leaves => Set<Leave>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();

    // ═══════════════════════════════════════
    // MUHASEBE MODULU (MUH-01)
    // ═══════════════════════════════════════
    public DbSet<ChartOfAccounts> ChartOfAccounts => Set<ChartOfAccounts>();
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<SettlementBatch> SettlementBatches => Set<SettlementBatch>();
    public DbSet<SettlementLine> SettlementLines => Set<SettlementLine>();
    public DbSet<CommissionRecord> CommissionRecords => Set<CommissionRecord>();
    public DbSet<CargoExpense> CargoExpenses => Set<CargoExpense>();
    public DbSet<Domain.Entities.ShipmentCost> ShipmentCosts => Set<Domain.Entities.ShipmentCost>();
    public DbSet<Domain.Accounting.Entities.AccountingPeriod> AccountingPeriods => Set<Domain.Accounting.Entities.AccountingPeriod>();
    public DbSet<BankTransaction> AccountingBankTransactions => Set<BankTransaction>();
    public DbSet<ReconciliationMatch> ReconciliationMatches => Set<ReconciliationMatch>();
    public DbSet<AccountingDocument> AccountingDocuments => Set<AccountingDocument>();
    public DbSet<PersonalExpense> PersonalExpenses => Set<PersonalExpense>();
    public DbSet<ExpenseCategory> AccountingExpenseCategories => Set<ExpenseCategory>();
    public DbSet<TaxRecord> TaxRecords => Set<TaxRecord>();
    public DbSet<TaxWithholding> TaxWithholdings => Set<TaxWithholding>();
    public DbSet<CashFlowEntry> CashFlowEntries => Set<CashFlowEntry>();
    public DbSet<ProfitReport> ProfitReports => Set<ProfitReport>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();
    public DbSet<AccountingSupplierAccount> AccountingSupplierAccounts => Set<AccountingSupplierAccount>();
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();

    // ═══════════════════════════════════════
    // DALGA N1 — MUHASEBE GENİŞLETME
    // ═══════════════════════════════════════
    public DbSet<SalaryRecord> SalaryRecords => Set<SalaryRecord>();
    public DbSet<FixedExpense> FixedExpenses => Set<FixedExpense>();
    public DbSet<PenaltyRecord> PenaltyRecords => Set<PenaltyRecord>();

    // ═══════════════════════════════════════
    // DALGA 9 — E-FATURA
    // ═══════════════════════════════════════
    public DbSet<EInvoiceDocument> EInvoiceDocuments => Set<EInvoiceDocument>();
    public DbSet<EInvoiceLine> EInvoiceLines => Set<EInvoiceLine>();
    public DbSet<EInvoiceSendLog> EInvoiceSendLogs => Set<EInvoiceSendLog>();

    // ═══════════════════════════════════════
    // DALGA 10 — SOSYAL TİCARET & ÖDEME
    // ═══════════════════════════════════════
    public DbSet<SocialFeedConfiguration> SocialFeedConfigurations => Set<SocialFeedConfiguration>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    // ═══════════════════════════════════════
    // DALGA 11 — ERP ENTEGRASYONU
    // ═══════════════════════════════════════
    public DbSet<ErpSyncLog> ErpSyncLogs => Set<ErpSyncLog>();
    public DbSet<ErpConflictLog> ErpConflictLogs => Set<ErpConflictLog>();

    // ═══════════════════════════════════════
    // DALGA 14 — MUHASEBE SABİT KIYMET + Ba/Bs
    // ═══════════════════════════════════════
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<BaBsRecord> BaBsRecords => Set<BaBsRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Fluent API Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Query Filters — soft-delete + tenant izolasyonu
        // NOT: Expression.Constant(value) kullanılmaz — model cache'de baked-in olur.
        // CurrentTenantId property'si EF Core tarafından query-time'da instance üzerinden değerlendirilir.
        var currentTenantIdProperty = typeof(AppDbContext)
            .GetProperty(nameof(CurrentTenantId),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var dbContextExpr = Expression.Constant(this, typeof(AppDbContext));
        var tenantIdCallExpr = Expression.Property(dbContextExpr, currentTenantIdProperty);

        // Platform-agnostic entity'ler — tenant filter UYGULANMAZ (admin tüm veriyi görebilir):
        // SyncLog: cross-tenant admin görünümü için ITenantEntity implement etmez
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var parameter = Expression.Parameter(clrType, "e");

            var isSoftDeletable = typeof(BaseEntity).IsAssignableFrom(clrType);
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(clrType);

            if (!isSoftDeletable && !isTenantEntity)
                continue;

            Expression? filter = null;

            // Soft-delete filter
            if (isSoftDeletable)
            {
                filter = Expression.Equal(
                    Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                    Expression.Constant(false));
            }

            // Tenant filter — CurrentTenantId property'si üzerinden dinamik değerlendirme
            if (isTenantEntity)
            {
                var tenantFilter = Expression.Equal(
                    Expression.Property(parameter, nameof(ITenantEntity.TenantId)),
                    tenantIdCallExpr);

                filter = filter == null ? tenantFilter : Expression.AndAlso(filter, tenantFilter);
            }

            if (filter != null)
            {
                modelBuilder.Entity(clrType)
                    .HasQueryFilter(Expression.Lambda(filter, parameter));
            }
        }

        // ── TenantId Indexes — performans (Global Query Filter her sorguya TenantId ekler) ──
        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.TenantId).HasDatabaseName("IX_Products_TenantId");
            e.HasIndex(p => p.SKU).IsUnique();
            e.HasIndex(p => p.Barcode);
            e.HasIndex(p => p.IsActive);
            e.Property(p => p.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasIndex(s => s.TenantId).HasDatabaseName("IX_StockMovements_TenantId");
            e.HasIndex(s => s.ProductId);
            e.HasIndex(s => s.Date);
            e.Property(s => s.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.TenantId).HasDatabaseName("IX_Orders_TenantId");
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.CustomerId);
            e.Property(o => o.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_Categories_TenantId");
            e.HasIndex(c => c.Code).IsUnique();
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_Customers_TenantId");
            e.Property(c => c.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Warehouse>(e =>
        {
            e.HasIndex(w => w.TenantId).HasDatabaseName("IX_Warehouses_TenantId");
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasIndex(s => s.TenantId).HasDatabaseName("IX_Suppliers_TenantId");
        });

        modelBuilder.Entity<InventoryLot>(e =>
        {
            e.HasIndex(l => l.TenantId).HasDatabaseName("IX_InventoryLots_TenantId");
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).HasFilter("\"Email\" IS NOT NULL");
        });

        // Invoice
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.HasIndex(i => i.OrderId);
            e.HasIndex(i => i.Status);
            e.Property(i => i.InvoiceNumber).HasMaxLength(50);
            e.Property(i => i.CustomerName).HasMaxLength(300);
            e.Property(i => i.CustomerTaxNumber).HasMaxLength(20);
            e.Property(i => i.CustomerTaxOffice).HasMaxLength(100);
            e.Property(i => i.CustomerAddress).HasMaxLength(500);
            e.Property(i => i.Currency).HasMaxLength(5);
            e.Property(i => i.PlatformCode).HasMaxLength(50);
            e.Property(i => i.PlatformOrderId).HasMaxLength(100);
            e.Property(i => i.GibInvoiceId).HasMaxLength(100);
            e.Property(i => i.GibEnvelopeId).HasMaxLength(100);

            e.HasMany(i => i.Lines)
                .WithOne(l => l.Invoice)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Store)
                .WithMany()
                .HasForeignKey(i => i.StoreId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // InvoiceLine
        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.Property(l => l.ProductName).HasMaxLength(300);
            e.Property(l => l.SKU).HasMaxLength(100);
            e.Property(l => l.Barcode).HasMaxLength(100);

            e.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // InvoiceTemplate — Dalga 5
        modelBuilder.Entity<InvoiceTemplate>(e =>
        {
            e.HasIndex(t => t.TenantId).HasDatabaseName("IX_InvoiceTemplates_TenantId");
            e.Property(t => t.TemplateName).HasMaxLength(100);
            e.Property(t => t.PhoneNumber).HasMaxLength(20);
            e.Property(t => t.Email).HasMaxLength(200);
            e.Property(t => t.TicaretSicilNo).HasMaxLength(50);
            e.Property(t => t.LogoImage).HasMaxLength(500_000);
            e.Property(t => t.SignatureImage).HasMaxLength(500_000);

            e.HasOne(t => t.Store)
                .WithMany()
                .HasForeignKey(t => t.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // KontorBalance — Dalga 5
        modelBuilder.Entity<KontorBalance>(e =>
        {
            e.HasIndex(k => k.TenantId).HasDatabaseName("IX_KontorBalances_TenantId");
            e.HasIndex(k => new { k.StoreId, k.Provider })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            e.HasOne(k => k.Store)
                .WithMany()
                .HasForeignKey(k => k.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Dalga 5: Multi-Tenant Gap Fix TenantId Indexes ──
        modelBuilder.Entity<StoreCredential>(e =>
        {
            e.HasIndex(sc => sc.TenantId).HasDatabaseName("IX_StoreCredentials_TenantId");
        });

        modelBuilder.Entity<ProductPlatformMapping>(e =>
        {
            e.HasIndex(pm => pm.TenantId).HasDatabaseName("IX_ProductPlatformMappings_TenantId");
        });

        modelBuilder.Entity<BrandPlatformMapping>(e =>
        {
            e.HasIndex(bm => bm.TenantId).HasDatabaseName("IX_BrandPlatformMappings_TenantId");
        });

        modelBuilder.Entity<CategoryPlatformMapping>(e =>
        {
            e.HasIndex(cm => cm.TenantId).HasDatabaseName("IX_CategoryPlatformMappings_TenantId");
        });

        modelBuilder.Entity<OfflineQueueItem>(e =>
        {
            e.HasIndex(o => o.TenantId).HasDatabaseName("IX_OfflineQueueItems_TenantId");
        });

        modelBuilder.Entity<SyncRetryItem>(e =>
        {
            e.HasIndex(s => s.TenantId).HasDatabaseName("IX_SyncRetryItems_TenantId");
        });

        // ── Dalga 4: Finansal Entity Index'leri ──

        // ReturnRequest
        modelBuilder.Entity<ReturnRequest>(e =>
        {
            e.HasIndex(r => r.TenantId).HasDatabaseName("IX_ReturnRequests_TenantId");
            e.HasIndex(r => r.OrderId);
            e.HasIndex(r => r.Status);
            e.HasIndex(r => new { r.TenantId, r.Platform, r.Status })
                .HasDatabaseName("IX_ReturnRequests_Tenant_Platform_Status");
            e.Property(r => r.RefundAmount).HasPrecision(18, 2);
            e.Property(r => r.CustomerName).HasMaxLength(300);
        });

        // AccountTransaction
        modelBuilder.Entity<AccountTransaction>(e =>
        {
            e.HasIndex(t => t.TenantId).HasDatabaseName("IX_AccountTransactions_TenantId");
            e.HasIndex(t => t.AccountId);
            e.HasIndex(t => t.TransactionDate);
            e.HasIndex(t => new { t.TenantId, t.Type })
                .HasDatabaseName("IX_AccountTransactions_Tenant_Type");
            e.Property(t => t.DebitAmount).HasPrecision(18, 2);
            e.Property(t => t.CreditAmount).HasPrecision(18, 2);
        });

        // CustomerAccount
        modelBuilder.Entity<CustomerAccount>(e =>
        {
            e.HasIndex(a => a.TenantId).HasDatabaseName("IX_CustomerAccounts_TenantId");
        });

        // SupplierAccount
        modelBuilder.Entity<SupplierAccount>(e =>
        {
            e.HasIndex(a => a.TenantId).HasDatabaseName("IX_SupplierAccounts_TenantId");
        });

        // PlatformCommission
        modelBuilder.Entity<PlatformCommission>(e =>
        {
            e.HasIndex(c => new { c.TenantId, c.Platform, c.PlatformCategoryId })
                .HasDatabaseName("IX_PlatformCommissions_Tenant_Platform_Category");
            e.Property(c => c.Rate).HasPrecision(5, 2);
            e.Property(c => c.MinAmount).HasPrecision(18, 2);
            e.Property(c => c.MaxAmount).HasPrecision(18, 2);
        });

        // PlatformPayment
        modelBuilder.Entity<PlatformPayment>(e =>
        {
            e.HasIndex(p => new { p.TenantId, p.Platform, p.Status })
                .HasDatabaseName("IX_PlatformPayments_Tenant_Platform_Status");
            e.HasIndex(p => new { p.TenantId, p.PeriodStart })
                .HasDatabaseName("IX_PlatformPayments_Tenant_Period");
            e.Property(p => p.GrossSales).HasPrecision(18, 2);
            e.Property(p => p.TotalCommission).HasPrecision(18, 2);
            e.Property(p => p.TotalShippingCost).HasPrecision(18, 2);
            e.Property(p => p.TotalReturnDeduction).HasPrecision(18, 2);
            e.Property(p => p.OtherDeductions).HasPrecision(18, 2);
            e.Property(p => p.NetAmount).HasPrecision(18, 2);
        });


        // AI — PriceRecommendation
        modelBuilder.Entity<PriceRecommendation>(e =>
        {
            e.HasIndex(r => new { r.ProductId, r.CreatedAt })
                .HasDatabaseName("IX_PriceRecommendations_Product_Created");
            e.Property(r => r.RecommendedPrice).HasPrecision(18, 2);
            e.Property(r => r.CurrentPrice).HasPrecision(18, 2);
            e.Property(r => r.CompetitorMinPrice).HasPrecision(18, 2);
        });

        // AI — StockPrediction
        modelBuilder.Entity<StockPrediction>(e =>
        {
            e.HasIndex(s => new { s.ProductId, s.CreatedAt })
                .HasDatabaseName("IX_StockPredictions_Product_Created");
        });

        // ── Dalga 5: OnMuhasebe Entity'leri (A-05) ──

        // Income
        modelBuilder.Entity<Income>(e =>
        {
            e.Property(i => i.Description).HasMaxLength(500);
            e.HasIndex(i => new { i.TenantId, i.Date })
                .HasDatabaseName("IX_Incomes_TenantId_Date");
        });

        // Expense
        modelBuilder.Entity<Expense>(e =>
        {
            e.Property(ex => ex.Description).HasMaxLength(500);
            e.HasIndex(ex => new { ex.TenantId, ex.Date })
                .HasDatabaseName("IX_Expenses_TenantId_Date");
        });

        // CariHesap
        modelBuilder.Entity<CariHesap>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.TaxNumber).HasMaxLength(20);
            e.Property(c => c.Phone).HasMaxLength(20);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.Address).HasMaxLength(500);
            e.HasIndex(c => new { c.TenantId, c.Type })
                .HasDatabaseName("IX_CariHesaplar_TenantId_Type");
        });

        // CariHareket
        modelBuilder.Entity<CariHareket>(e =>
        {
            e.Property(h => h.Description).HasMaxLength(500);
            e.HasIndex(h => new { h.TenantId, h.CariHesapId })
                .HasDatabaseName("IX_CariHareketler_TenantId_CariHesapId");

            e.HasOne(h => h.CariHesap)
                .WithMany(c => c.Hareketler)
                .HasForeignKey(h => h.CariHesapId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── G9 C-07: ProductVariant — flexible JSON attributes ──
        modelBuilder.Entity<ProductVariant>(b =>
        {
            b.HasKey(v => v.Id);
            b.Property(v => v.SKU).HasMaxLength(100).IsRequired();
            b.Property(v => v.Stock).HasDefaultValue(0);
            b.HasIndex(v => v.SKU).IsUnique();
            b.HasIndex(v => v.ProductId);

            // JSON value conversion for attributes (SQLite TEXT / PostgreSQL TEXT)
            b.Property(v => v.AttributesJson)
             .HasColumnName("Attributes")
             .HasDefaultValue("{}");
        });

        // ── G7 C-05: ProductSet ──
        modelBuilder.Entity<ProductSet>(e =>
        {
            e.Property(ps => ps.Name).HasMaxLength(200);
            e.Property(ps => ps.Description).HasMaxLength(500);
            e.HasIndex(ps => ps.TenantId).HasDatabaseName("IX_ProductSets_TenantId");

            e.HasMany(ps => ps.Items)
                .WithOne()
                .HasForeignKey(psi => psi.ProductSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductSetItem
        modelBuilder.Entity<ProductSetItem>(e =>
        {
            e.Property(psi => psi.Quantity).HasDefaultValue(1);
        });

        // ── Dalga 6: Quotation ──
        modelBuilder.Entity<Quotation>(e =>
        {
            e.HasIndex(q => q.QuotationNumber).IsUnique();
            e.HasIndex(q => new { q.TenantId, q.Status });
            e.Property(q => q.QuotationNumber).HasMaxLength(50);
            e.Property(q => q.CustomerName).HasMaxLength(300);
            e.Property(q => q.SubTotal).HasPrecision(18, 2);
            e.Property(q => q.TaxTotal).HasPrecision(18, 2);
            e.Property(q => q.GrandTotal).HasPrecision(18, 2);
            e.Property(q => q.Currency).HasMaxLength(3);

            e.HasMany(q => q.Lines)
                .WithOne(l => l.Quotation)
                .HasForeignKey(l => l.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuotationLine
        modelBuilder.Entity<QuotationLine>(e =>
        {
            e.Property(l => l.UnitPrice).HasPrecision(18, 2);
            e.Property(l => l.TaxRate).HasPrecision(5, 2);
            e.Property(l => l.ProductName).HasMaxLength(300);
            e.Property(l => l.SKU).HasMaxLength(100);
            e.Ignore(l => l.TaxAmount);
            e.Ignore(l => l.LineTotal);
        });

        // ═══════════════════════════════════════
        // DALGA 8 — CRM CONFIG
        // ═══════════════════════════════════════

        modelBuilder.Entity<Pipeline>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(p => p.TenantId).HasDatabaseName("IX_Pipelines_TenantId");
            e.HasMany(p => p.Stages).WithOne(s => s.Pipeline)
             .HasForeignKey(s => s.PipelineId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PipelineStage>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();
            e.Property(s => s.Color).HasMaxLength(7);
            e.HasIndex(s => s.TenantId).HasDatabaseName("IX_PipelineStages_TenantId");
        });

        modelBuilder.Entity<Lead>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.FullName).HasMaxLength(200).IsRequired();
            e.Property(l => l.Email).HasMaxLength(200);
            e.Property(l => l.Phone).HasMaxLength(30);
            e.Property(l => l.Company).HasMaxLength(200);
            e.Property(l => l.Notes).HasMaxLength(2000);
            e.HasIndex(l => l.TenantId).HasDatabaseName("IX_Leads_TenantId");
            e.HasIndex(l => new { l.TenantId, l.Status }).HasDatabaseName("IX_Leads_TenantId_Status");
        });

        modelBuilder.Entity<CrmContact>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.FullName).HasMaxLength(200).IsRequired();
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(30);
            e.Property(c => c.TaxNumber).HasMaxLength(11);
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_CrmContacts_TenantId");
            // Customer FK — nullable, Customer silinirse null olur
            e.HasOne<Customer>().WithMany()
             .HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        // ═══════════════════════════════════════
        // DALGA 8 — DEAL & ACTIVITY CONFIG
        // ═══════════════════════════════════════
        modelBuilder.Entity<Deal>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Title).HasMaxLength(200).IsRequired();
            e.Property(d => d.Amount).HasPrecision(18, 2);
            e.Property(d => d.Currency).HasMaxLength(3);
            e.Property(d => d.LostReason).HasMaxLength(1000);
            e.HasIndex(d => d.TenantId).HasDatabaseName("IX_Deals_TenantId");
            e.HasIndex(d => new { d.TenantId, d.PipelineId, d.Status }).HasDatabaseName("IX_Deals_Tenant_Pipeline_Status");
            e.HasOne(d => d.Pipeline).WithMany().HasForeignKey(d => d.PipelineId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Stage).WithMany().HasForeignKey(d => d.StageId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Contact).WithMany().HasForeignKey(d => d.CrmContactId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        modelBuilder.Entity<Activity>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Subject).HasMaxLength(300).IsRequired();
            e.Property(a => a.Description).HasMaxLength(2000);
            e.HasIndex(a => a.TenantId).HasDatabaseName("IX_Activities_TenantId");
            e.HasIndex(a => new { a.TenantId, a.DealId }).HasDatabaseName("IX_Activities_Tenant_Deal");
        });

        // ═══════════════════════════════════════
        // DALGA 8 — FİNANS CONFIG
        // ═══════════════════════════════════════
        modelBuilder.Entity<BankAccount>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.AccountName).HasMaxLength(200).IsRequired();
            e.Property(b => b.BankName).HasMaxLength(100);
            e.Property(b => b.IBAN).HasMaxLength(34);
            e.Property(b => b.AccountNumber).HasMaxLength(50);
            e.Property(b => b.Currency).HasMaxLength(3);
            e.Property(b => b.Balance).HasPrecision(18, 2);
            e.HasIndex(b => b.TenantId).HasDatabaseName("IX_BankAccounts_TenantId");
        });

        modelBuilder.Entity<GLTransaction>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Description).HasMaxLength(500).IsRequired();
            e.Property(g => g.Currency).HasMaxLength(3);
            e.Property(g => g.Amount).HasPrecision(18, 2);
            e.Property(g => g.ExchangeRate).HasPrecision(10, 6);
            e.Property(g => g.ReferenceNumber).HasMaxLength(100);
            e.HasIndex(g => g.TenantId).HasDatabaseName("IX_GLTransactions_TenantId");
            e.HasIndex(g => new { g.TenantId, g.TransactionDate }).HasDatabaseName("IX_GLTransactions_Tenant_Date");
        });

        // ═══════════════════════════════════════
        // DALGA 8 — GÖREVLER & PROJELER CONFIG
        // ═══════════════════════════════════════
        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Color).HasMaxLength(7);
            e.HasIndex(p => p.TenantId).HasDatabaseName("IX_Projects_TenantId");
        });

        modelBuilder.Entity<Milestone>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(m => m.TenantId).HasDatabaseName("IX_Milestones_TenantId");
            e.HasIndex(m => m.ProjectId);
        });

        modelBuilder.Entity<WorkTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(300).IsRequired();
            e.Property(t => t.Tags).HasMaxLength(500);
            e.HasIndex(t => t.TenantId).HasDatabaseName("IX_WorkTasks_TenantId");
            e.HasIndex(t => new { t.TenantId, t.Status }).HasDatabaseName("IX_WorkTasks_Tenant_Status");
        });

        modelBuilder.Entity<TimeEntry>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.HourlyRate).HasPrecision(18, 2);
            e.HasIndex(t => t.TenantId).HasDatabaseName("IX_TimeEntries_TenantId");
            e.HasIndex(t => t.WorkTaskId);
        });

        // ═══════════════════════════════════════
        // DALGA 8 — TAKVİM CONFIG
        // ═══════════════════════════════════════
        modelBuilder.Entity<CalendarEvent>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Title).HasMaxLength(300).IsRequired();
            e.Property(c => c.Location).HasMaxLength(300);
            e.Property(c => c.Color).HasMaxLength(7);
            e.Property(c => c.RecurrenceRule).HasMaxLength(500);
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_CalendarEvents_TenantId");
            e.HasIndex(c => new { c.TenantId, c.StartAt }).HasDatabaseName("IX_CalendarEvents_Tenant_Start");
        });

        modelBuilder.Entity<CalendarEventAttendee>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.CalendarEventId, a.UserId }).IsUnique().HasDatabaseName("IX_CalendarEventAttendees_Event_User");
        });

        // ═══════════════════════════════════════
        // DALGA 8 — BELGELER CONFIG
        // ═══════════════════════════════════════
        modelBuilder.Entity<DocumentFolder>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(f => f.TenantId).HasDatabaseName("IX_DocumentFolders_TenantId");
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.FileName).HasMaxLength(500).IsRequired();
            e.Property(d => d.OriginalFileName).HasMaxLength(500);
            e.Property(d => d.ContentType).HasMaxLength(100);
            e.Property(d => d.StoragePath).HasMaxLength(1000);
            e.Property(d => d.Tags).HasMaxLength(500);
            e.HasIndex(d => d.TenantId).HasDatabaseName("IX_Documents_TenantId");
            e.HasIndex(d => new { d.TenantId, d.FolderId }).HasDatabaseName("IX_Documents_Tenant_Folder");
        });

        // ═══════════════════════════════════════
        // DALGA 8 — İNSAN KAYNAKLARI CONFIG
        // ═══════════════════════════════════════
        modelBuilder.Entity<Department>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(d => d.TenantId).HasDatabaseName("IX_Departments_TenantId");
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(em => em.Id);
            e.Property(em => em.EmployeeCode).HasMaxLength(50).IsRequired();
            e.Property(em => em.JobTitle).HasMaxLength(100);
            e.Property(em => em.WorkEmail).HasMaxLength(200);
            e.Property(em => em.WorkPhone).HasMaxLength(30);
            e.Property(em => em.HourlyRate).HasPrecision(18, 2);
            e.Property(em => em.MonthlySalary).HasPrecision(18, 2);
            e.HasIndex(em => em.TenantId).HasDatabaseName("IX_Employees_TenantId");
            e.HasIndex(em => new { em.TenantId, em.EmployeeCode }).IsUnique().HasDatabaseName("IX_Employees_Tenant_Code");
        });

        modelBuilder.Entity<Leave>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.TenantId).HasDatabaseName("IX_Leaves_TenantId");
            e.HasIndex(l => new { l.TenantId, l.EmployeeId, l.Status }).HasDatabaseName("IX_Leaves_Tenant_Employee_Status");
        });

        modelBuilder.Entity<WorkSchedule>(e =>
        {
            e.HasKey(ws => ws.Id);
            e.Property(ws => ws.Notes).HasMaxLength(500);
            e.HasIndex(ws => ws.TenantId).HasDatabaseName("IX_WorkSchedules_TenantId");
            e.HasIndex(ws => new { ws.EmployeeId, ws.DayOfWeek }).IsUnique().HasDatabaseName("IX_WorkSchedules_Employee_Day");
        });

        // ═══════════════════════════════════════
        // MUHASEBE MODULU (MUH-01) CONFIG
        // ═══════════════════════════════════════

        modelBuilder.Entity<ChartOfAccounts>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Code).HasMaxLength(20).IsRequired();
            e.Property(a => a.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(a => a.TenantId).HasDatabaseName("IX_ChartOfAccounts_TenantId");
            e.HasIndex(a => new { a.TenantId, a.Code }).IsUnique().HasDatabaseName("IX_ChartOfAccounts_Tenant_Code");
            e.HasOne(a => a.Parent).WithMany().HasForeignKey(a => a.ParentId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        });

        modelBuilder.Entity<Counterparty>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.VKN).HasMaxLength(20);
            e.Property(c => c.Phone).HasMaxLength(30);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.Address).HasMaxLength(500);
            e.Property(c => c.Platform).HasMaxLength(50);
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_Counterparties_TenantId");
            e.HasIndex(c => new { c.TenantId, c.CounterpartyType }).HasDatabaseName("IX_Counterparties_Tenant_Type");
        });

        modelBuilder.Entity<JournalEntry>(e =>
        {
            e.HasKey(j => j.Id);
            e.Property(j => j.Description).HasMaxLength(500).IsRequired();
            e.Property(j => j.ReferenceNumber).HasMaxLength(100);
            e.HasIndex(j => j.TenantId).HasDatabaseName("IX_JournalEntries_TenantId");
            e.HasIndex(j => new { j.TenantId, j.EntryDate }).HasDatabaseName("IX_JournalEntries_Tenant_Date");
            e.HasMany(j => j.Lines).WithOne(l => l.JournalEntry).HasForeignKey(l => l.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JournalLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Debit).HasPrecision(18, 2);
            e.Property(l => l.Credit).HasPrecision(18, 2);
            e.Property(l => l.Description).HasMaxLength(500);
            e.HasOne(l => l.Account).WithMany().HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SettlementBatch>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Platform).HasMaxLength(50).IsRequired();
            e.Property(s => s.TotalGross).HasPrecision(18, 2);
            e.Property(s => s.TotalCommission).HasPrecision(18, 2);
            e.Property(s => s.TotalNet).HasPrecision(18, 2);
            e.HasIndex(s => s.TenantId).HasDatabaseName("IX_SettlementBatches_TenantId");
            e.HasIndex(s => new { s.TenantId, s.Platform, s.PeriodStart }).HasDatabaseName("IX_SettlementBatches_Tenant_Platform_Period");
            e.HasMany(s => s.Lines).WithOne(l => l.SettlementBatch).HasForeignKey(l => l.SettlementBatchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SettlementLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.OrderId).HasMaxLength(100);
            e.Property(l => l.GrossAmount).HasPrecision(18, 2);
            e.Property(l => l.CommissionAmount).HasPrecision(18, 2);
            e.Property(l => l.ServiceFee).HasPrecision(18, 2);
            e.Property(l => l.CargoDeduction).HasPrecision(18, 2);
            e.Property(l => l.RefundDeduction).HasPrecision(18, 2);
            e.Property(l => l.NetAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CommissionRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Platform).HasMaxLength(50).IsRequired();
            e.Property(c => c.OrderId).HasMaxLength(100);
            e.Property(c => c.Category).HasMaxLength(200);
            e.Property(c => c.GrossAmount).HasPrecision(18, 2);
            e.Property(c => c.CommissionRate).HasPrecision(5, 4);
            e.Property(c => c.CommissionAmount).HasPrecision(18, 2);
            e.Property(c => c.ServiceFee).HasPrecision(18, 2);
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_CommissionRecords_TenantId");
            e.HasIndex(c => new { c.TenantId, c.Platform }).HasDatabaseName("IX_CommissionRecords_Tenant_Platform");
        });

        modelBuilder.Entity<CargoExpense>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CarrierName).HasMaxLength(100).IsRequired();
            e.Property(c => c.OrderId).HasMaxLength(100);
            e.Property(c => c.TrackingNumber).HasMaxLength(100);
            e.Property(c => c.Cost).HasPrecision(18, 2);
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_CargoExpenses_TenantId");
        });

        modelBuilder.Entity<BankTransaction>(e =>
        {
            e.ToTable("AccountingBankTransactions");
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Description).HasMaxLength(500).IsRequired();
            e.Property(t => t.ReferenceNumber).HasMaxLength(100);
            e.Property(t => t.IdempotencyKey).HasMaxLength(100);
            e.HasIndex(t => t.TenantId).HasDatabaseName("IX_AccountingBankTransactions_TenantId");
            e.HasIndex(t => new { t.TenantId, t.IdempotencyKey }).IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL")
                .HasDatabaseName("IX_AccountingBankTransactions_Tenant_IdempotencyKey");
        });

        modelBuilder.Entity<ReconciliationMatch>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Confidence).HasPrecision(5, 4);
            e.Property(m => m.ReviewedBy).HasMaxLength(200);
            e.HasIndex(m => m.TenantId).HasDatabaseName("IX_ReconciliationMatches_TenantId");
            e.HasIndex(m => new { m.TenantId, m.Status }).HasDatabaseName("IX_ReconciliationMatches_Tenant_Status");
        });

        modelBuilder.Entity<AccountingDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.FileName).HasMaxLength(500).IsRequired();
            e.Property(d => d.MimeType).HasMaxLength(100);
            e.Property(d => d.StoragePath).HasMaxLength(1000).IsRequired();
            e.Property(d => d.ExtractedData).HasColumnType("text");
            e.HasIndex(d => d.TenantId).HasDatabaseName("IX_AccountingDocuments_TenantId");
            e.HasIndex(d => new { d.TenantId, d.DocumentType }).HasDatabaseName("IX_AccountingDocuments_Tenant_Type");
            e.HasOne(d => d.Counterparty).WithMany().HasForeignKey(d => d.CounterpartyId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        modelBuilder.Entity<PersonalExpense>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).HasMaxLength(300).IsRequired();
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.Category).HasMaxLength(200);
            e.Property(p => p.ApprovedBy).HasMaxLength(200);
            e.HasIndex(p => p.TenantId).HasDatabaseName("IX_PersonalExpenses_TenantId");
            e.HasIndex(p => new { p.TenantId, p.ExpenseDate }).HasDatabaseName("IX_PersonalExpenses_Tenant_Date");
        });

        modelBuilder.Entity<ExpenseCategory>(e =>
        {
            e.ToTable("AccountingExpenseCategories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Code).HasMaxLength(20);
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_AccountingExpenseCategories_TenantId");
            e.HasOne(c => c.Parent).WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        });

        modelBuilder.Entity<TaxRecord>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Period).HasMaxLength(20).IsRequired();
            e.Property(t => t.TaxType).HasMaxLength(50).IsRequired();
            e.Property(t => t.TaxableAmount).HasPrecision(18, 2);
            e.Property(t => t.TaxAmount).HasPrecision(18, 2);
            e.HasIndex(t => t.TenantId).HasDatabaseName("IX_TaxRecords_TenantId");
            e.HasIndex(t => new { t.TenantId, t.Period }).HasDatabaseName("IX_TaxRecords_Tenant_Period");
        });

        modelBuilder.Entity<TaxWithholding>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.TaxExclusiveAmount).HasPrecision(18, 2);
            e.Property(w => w.Rate).HasPrecision(5, 4);
            e.Property(w => w.WithholdingAmount).HasPrecision(18, 2);
            e.Property(w => w.TaxType).HasMaxLength(50).IsRequired();
            e.HasIndex(w => w.TenantId).HasDatabaseName("IX_TaxWithholdings_TenantId");
            e.HasIndex(w => w.InvoiceId).HasDatabaseName("IX_TaxWithholdings_InvoiceId");
        });

        modelBuilder.Entity<CashFlowEntry>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Amount).HasPrecision(18, 2);
            e.Property(c => c.Category).HasMaxLength(200);
            e.Property(c => c.Description).HasMaxLength(500);
            e.HasIndex(c => c.TenantId).HasDatabaseName("IX_CashFlowEntries_TenantId");
            e.HasIndex(c => new { c.TenantId, c.EntryDate, c.Direction }).HasDatabaseName("IX_CashFlowEntries_Tenant_Date_Dir");
            e.HasOne(c => c.Counterparty).WithMany().HasForeignKey(c => c.CounterpartyId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        modelBuilder.Entity<ProfitReport>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Period).HasMaxLength(20).IsRequired();
            e.Property(r => r.Platform).HasMaxLength(50);
            e.Property(r => r.TotalRevenue).HasPrecision(18, 2);
            e.Property(r => r.TotalCost).HasPrecision(18, 2);
            e.Property(r => r.TotalCommission).HasPrecision(18, 2);
            e.Property(r => r.TotalCargo).HasPrecision(18, 2);
            e.Property(r => r.TotalTax).HasPrecision(18, 2);
            e.Property(r => r.NetProfit).HasPrecision(18, 2);
            e.HasIndex(r => r.TenantId).HasDatabaseName("IX_ProfitReports_TenantId");
            e.HasIndex(r => new { r.TenantId, r.Period }).HasDatabaseName("IX_ProfitReports_Tenant_Period");
        });

        modelBuilder.Entity<FinancialGoal>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Title).HasMaxLength(300).IsRequired();
            e.Property(g => g.TargetAmount).HasPrecision(18, 2);
            e.Property(g => g.CurrentAmount).HasPrecision(18, 2);
            e.HasIndex(g => g.TenantId).HasDatabaseName("IX_FinancialGoals_TenantId");
        });

        modelBuilder.Entity<AccountingSupplierAccount>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).HasMaxLength(200).IsRequired();
            e.Property(a => a.Balance).HasPrecision(18, 2);
            e.Property(a => a.Currency).HasMaxLength(3);
            e.HasIndex(a => a.TenantId).HasDatabaseName("IX_AccountingSupplierAccounts_TenantId");
            e.HasIndex(a => new { a.TenantId, a.SupplierId }).IsUnique().HasDatabaseName("IX_AccountingSupplierAccounts_Tenant_Supplier");
        });

        modelBuilder.Entity<LegalEntity>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Name).HasMaxLength(200).IsRequired();
            e.Property(l => l.TaxNumber).HasMaxLength(20).IsRequired();
            e.Property(l => l.Address).HasMaxLength(500);
            e.Property(l => l.Phone).HasMaxLength(30);
            e.Property(l => l.Email).HasMaxLength(200);
            e.HasIndex(l => l.TenantId).HasDatabaseName("IX_LegalEntities_TenantId");
        });

        // ═══════════════════════════════════════
        // KVKK — HASSAS ALAN SIFRELEME (MUH-02)
        // ═══════════════════════════════════════
        ConfigureEncryption(modelBuilder);
    }

    /// <summary>
    /// KVKK kapsamindaki hassas alanlara EF Core Value Converter uygular.
    /// BankAccount.IBAN ve Counterparty.VKN DB'de AES-256-GCM ile sifrelenir.
    /// Encryption servisi yoksa converter eklenmez (gelistirme ortami uyumu).
    /// </summary>
    private void ConfigureEncryption(ModelBuilder modelBuilder)
    {
        if (_encryptionService == null)
            return;

        var nullableConverter = new NullableEncryptedStringConverter(_encryptionService);

        // BankAccount.IBAN — KVKK hassas veri
        modelBuilder.Entity<BankAccount>(e =>
        {
            e.Property(b => b.IBAN).HasConversion(nullableConverter);
        });

        // BankAccount.AccountNumber — KVKK hassas veri
        modelBuilder.Entity<BankAccount>(e =>
        {
            e.Property(b => b.AccountNumber).HasConversion(nullableConverter);
        });

        // Counterparty.VKN — KVKK hassas veri (Vergi Kimlik Numarasi)
        modelBuilder.Entity<Counterparty>(e =>
        {
            e.Property(c => c.VKN).HasConversion(nullableConverter);
        });

        // LegalEntity.TaxNumber — KVKK hassas veri
        modelBuilder.Entity<LegalEntity>(e =>
        {
            e.Property(l => l.TaxNumber).HasConversion(
                new EncryptedStringConverter(_encryptionService));
        });
    }

    /// <summary>
    /// Domain event'leri toplar — UnitOfWork dispatch eder.
    /// </summary>
    public IReadOnlyList<IDomainEvent> GetDomainEvents()
    {
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());
        return events.AsReadOnly();
    }
}
