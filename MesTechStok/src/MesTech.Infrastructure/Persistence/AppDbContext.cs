using System.Linq.Expressions;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext — PostgreSQL + Domain Entity'ler + Global Tenant Filter.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
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

    // ── Dalga 4: Dropshipping ──
    public DbSet<SupplierFeed> SupplierFeeds => Set<SupplierFeed>();

    // ── Dalga 4: AI ──
    public DbSet<PriceRecommendation> PriceRecommendations => Set<PriceRecommendation>();
    public DbSet<StockPrediction> StockPredictions => Set<StockPrediction>();

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

        // SupplierFeed
        modelBuilder.Entity<SupplierFeed>(e =>
        {
            e.HasIndex(f => new { f.TenantId, f.SupplierId })
                .HasDatabaseName("IX_SupplierFeeds_Tenant_Supplier");
            e.Property(f => f.PriceMarkupPercent).HasPrecision(5, 2);
            e.Property(f => f.PriceMarkupFixed).HasPrecision(18, 2);
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
