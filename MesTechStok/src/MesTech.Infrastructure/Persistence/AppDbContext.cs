using System.Linq.Expressions;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Fluent API Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Query Filters — soft-delete + tenant izolasyonu
        var currentTenantId = _tenantProvider.GetCurrentTenantId();
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

            // Tenant filter
            if (isTenantEntity)
            {
                var tenantFilter = Expression.Equal(
                    Expression.Property(parameter, nameof(ITenantEntity.TenantId)),
                    Expression.Constant(currentTenantId));

                filter = filter == null ? tenantFilter : Expression.AndAlso(filter, tenantFilter);
            }

            if (filter != null)
            {
                modelBuilder.Entity(clrType)
                    .HasQueryFilter(Expression.Lambda(filter, parameter));
            }
        }

        // Product
        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.SKU).IsUnique();
            e.HasIndex(p => p.Barcode);
            e.HasIndex(p => p.IsActive);
            e.Property(p => p.RowVersion).IsRowVersion();
        });

        // StockMovement
        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasIndex(s => s.ProductId);
            e.HasIndex(s => s.Date);
            e.Property(s => s.RowVersion).IsRowVersion();
        });

        // Order
        modelBuilder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.CustomerId);
            e.Property(o => o.RowVersion).IsRowVersion();
        });

        // Category
        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).HasFilter("\"Email\" IS NOT NULL");
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
