using Microsoft.EntityFrameworkCore;
using MesTechStok.Core.Data.Models;
// AI Models will be added after migration - namespace conflict resolved
// using MesTechStok.Core.Models;
using BCrypt.Net;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MesTechStok.Core.Data;

/// <summary>
/// Ana veritabanı context sınıfı
/// Tüm veritabanı işlemleri bu sınıf üzerinden gerçekleştirilir
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    // Stok Yerleşim Sistemi DbSets - TEMPORARILY DISABLED
    // public DbSet<WarehouseZone> WarehouseZones { get; set; }
    // public DbSet<WarehouseRack> WarehouseRacks { get; set; }
    // public DbSet<WarehouseShelf> WarehouseShelves { get; set; }
    // public DbSet<WarehouseBin> WarehouseBins { get; set; }
    // public DbSet<ProductLocation> ProductLocations { get; set; }
    // public DbSet<LocationMovement> LocationMovements { get; set; }

    // Authentication & Authorization DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<AccessLog> AccessLogs { get; set; }
    public DbSet<CompanySettings> CompanySettings { get; set; }
    public DbSet<OfflineQueueItem> OfflineQueue { get; set; }
    public DbSet<ApiCallLog> ApiCallLogs { get; set; }
    public DbSet<CircuitStateLog> CircuitStateLogs { get; set; }
    public DbSet<LogEntry> LogEntries { get; set; }
    public DbSet<BarcodeScanLog> BarcodeScanLogs { get; set; }
    public DbSet<InventoryLot> InventoryLots { get; set; }
    public DbSet<SyncRetryItem> SyncRetryItems { get; set; }

    // AI Configuration DbSets - A++++ Enterprise AI Integration  
    public DbSet<MesTechStok.Core.Data.Models.AIConfiguration> AIConfigurations { get; set; }
    public DbSet<MesTechStok.Core.Data.Models.AIUsageLog> AIUsageLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // FAZ 1 GÖREV 1.1: Authentication Model Configurations
        ConfigureAuthenticationModels(modelBuilder);

        // Domain modelleri (ürün/kategori/sipariş/hareket) için kısıtlar ve indeksler
        ConfigureDomainModels(modelBuilder);

        // Company Settings - single row constraint via unique index hack
        modelBuilder.Entity<CompanySettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
        });

        // AI Configuration Models - A++++ Enterprise Integration
        ConfigureAIModels(modelBuilder);

        // Stok Yerleşim Sistemi Model Konfigürasyonları - TEMPORARILY DISABLED
        // ConfigureLocationModels(modelBuilder);

        // OfflineQueue
        modelBuilder.Entity<OfflineQueueItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Direction).IsRequired().HasMaxLength(16);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(16);
            entity.Property(e => e.Payload).HasColumnType("nvarchar(max)");
            entity.Property(e => e.LastError).HasMaxLength(4000);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.HasIndex(e => new { e.Status, e.NextAttemptAt });
            entity.HasIndex(e => e.CreatedDate);
        });

        // ApiCallLog (Telemetry persistence)
        modelBuilder.Entity<ApiCallLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Category).HasMaxLength(32);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => new { e.Endpoint, e.TimestampUtc });
        });

        // CircuitStateLog (Circuit breaker state transitions)
        modelBuilder.Entity<CircuitStateLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PreviousState).IsRequired().HasMaxLength(20);
            entity.Property(e => e.NewState).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.AdditionalInfo).HasMaxLength(256);
            entity.HasIndex(e => e.TransitionTimeUtc);
            entity.HasIndex(e => new { e.NewState, e.TransitionTimeUtc });
        });

        // BarcodeScanLog (Barcode scanning persistence)
        modelBuilder.Entity<BarcodeScanLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Barcode).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Format).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(16);
            entity.Property(e => e.DeviceId).HasMaxLength(64);
            entity.Property(e => e.ValidationMessage).HasMaxLength(256);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => new { e.Format, e.TimestampUtc });
        });

        // SyncRetryItem
        modelBuilder.Entity<SyncRetryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SyncType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ItemId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ItemType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ItemData).HasMaxLength(1000);
            entity.Property(e => e.LastError).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ErrorCategory).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.AdditionalInfo).HasMaxLength(200);
            entity.HasIndex(e => new { e.SyncType, e.IsResolved, e.NextRetryUtc });
        });
    }

    private void ConfigureDomainModels(ModelBuilder modelBuilder)
    {
        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Barcode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UsageInstructions).HasMaxLength(1000);
            entity.Property(e => e.ImporterInfo).HasMaxLength(255);
            entity.Property(e => e.ManufacturerInfo).HasMaxLength(255);

            // PRD gereği benzersiz alanlar
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Barcode).IsUnique();

            // Performans için ek indeksler
            entity.HasIndex(e => new { e.Name, e.IsActive });
            entity.HasIndex(e => new { e.CategoryId, e.IsActive });
            entity.HasIndex(e => new { e.WarehouseId, e.IsActive });
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).HasMaxLength(50);
            // PRD: Category.Code benzersiz
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            // PRD: OrderNumber benzersiz
            entity.HasIndex(e => e.OrderNumber).IsUnique();
        });

        // StockMovement
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            // PRD: (ProductId, Date) indeks
            entity.HasIndex(e => new { e.ProductId, e.Date });
        });

        // InventoryLot configuration
        modelBuilder.Entity<InventoryLot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LotNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.ProductId, e.Status, e.ExpiryDate });
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.InventoryLots)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAuthenticationModels(ModelBuilder modelBuilder)
    {
        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100); // Email nullable olarak değiştirildi
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL"); // Nullable için unique index filter
        });

        // Role Configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Permission Configuration
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Module).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => new { e.Name, e.Module }).IsUnique();
        });

        // UserRole Configuration  
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // RolePermission Configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Permission)
                  .WithMany()
                  .HasForeignKey(e => e.PermissionId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.GrantedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.GrantedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    // FAZ 1 GÖREV 1.1: Seed Authentication Data - TEMPORARILY DISABLED
    public async Task SeedAuthenticationDataAsync()
    {
        // EMERGENCY FIX: Skip authentication seeding to prevent Users table error
        return;

        // Check if authentication data already exists
        if (await Users.AnyAsync() || await Roles.AnyAsync())
            return;

        // Create Roles
        var adminRole = new Role
        {
            Name = "Admin",
            Description = "System Administrator",
            IsActive = true,
            IsSystemRole = true,
            CreatedDate = DateTime.Now
        };

        var managerRole = new Role
        {
            Name = "Manager",
            Description = "Warehouse Manager",
            IsActive = true,
            IsSystemRole = false,
            CreatedDate = DateTime.Now
        };

        var staffRole = new Role
        {
            Name = "Staff",
            Description = "Warehouse Staff",
            IsActive = true,
            IsSystemRole = false,
            CreatedDate = DateTime.Now
        };

        Roles.AddRange(adminRole, managerRole, staffRole);
        await SaveChangesAsync();

        // Create Permissions (baseline)
        var permissions = new[]
        {
            new Permission { Name = "ViewProducts", Module = "Products", Description = "View products", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "CreateProducts", Module = "Products", Description = "Create products", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "EditProducts", Module = "Products", Description = "Edit products", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "DeleteProducts", Module = "Products", Description = "Delete products", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "ViewOrders", Module = "Orders", Description = "View orders", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "CreateOrders", Module = "Orders", Description = "Create orders", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "ViewUsers", Module = "Users", Description = "View users", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "ManageUsers", Module = "Users", Description = "Manage users", IsActive = true, CreatedDate = DateTime.Now }
        };

        Permissions.AddRange(permissions);
        await SaveChangesAsync();

        // Additional permissions to align with UI authorization checks
        var extraPermissions = new[]
        {
            // Products module CRUD extras
            new Permission { Name = "Create", Module = "Products", Description = "Create products (UI)", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "Edit", Module = "Products", Description = "Edit products (UI)", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "Delete", Module = "Products", Description = "Delete products (UI)", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "UpdateStock", Module = "Products", Description = "Update product stock", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "UpdatePrice", Module = "Products", Description = "Update product price", IsActive = true, CreatedDate = DateTime.Now },

            // Orders module actions
            new Permission { Name = "Create", Module = "Orders", Description = "Create orders (UI)", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "Edit", Module = "Orders", Description = "Edit orders (UI)", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "Cancel", Module = "Orders", Description = "Cancel orders (UI)", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "UpdateStatus", Module = "Orders", Description = "Update order status (UI)", IsActive = true, CreatedDate = DateTime.Now },

            // Inventory module actions
            new Permission { Name = "Add", Module = "Inventory", Description = "Add stock", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "Remove", Module = "Inventory", Description = "Remove stock", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "Transfer", Module = "Inventory", Description = "Transfer stock", IsActive = true, CreatedDate = DateTime.Now },
            new Permission { Name = "Export", Module = "Inventory", Description = "Export inventory data", IsActive = true, CreatedDate = DateTime.Now },

            // Reports module actions
            new Permission { Name = "Export", Module = "Reports", Description = "Export reports", IsActive = true, CreatedDate = DateTime.Now }
        };

        // Insert only non-existing (Name+Module unique)
        foreach (var p in extraPermissions)
        {
            if (!await Permissions.AnyAsync(x => x.Name == p.Name && x.Module == p.Module))
            {
                Permissions.Add(p);
            }
        }
        await SaveChangesAsync();

        // Create Admin User
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@mestech.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedDate = DateTime.Now
        };

        Users.Add(adminUser);
        await SaveChangesAsync();

        // Assign Admin Role to Admin User
        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            AssignedDate = DateTime.Now
        };

        UserRoles.Add(userRole);

        // Assign All Permissions to Admin Role (baseline + extra)
        var allPerms = await Permissions.ToListAsync();
        var rolePermissions = allPerms.Select(p => new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = p.Id,
            GrantedDate = DateTime.Now
        }).ToArray();

        RolePermissions.AddRange(rolePermissions);
        await SaveChangesAsync();
    }

    /// <summary>
    /// Demo verileri ile sistemi doldurur. Üretimde bir defa çalışır; mevcut veri varsa etkisizdir.
    /// Boş ekranları önlemek ve testleri kolaylaştırmak için temel tabloları anlamlı kayıtlarla besler.
    /// </summary>
    public async Task SeedDemoDataAsync()
    {

        // 1) CompanySettings
        if (!await CompanySettings.AnyAsync())
        {
            CompanySettings.Add(new CompanySettings
            {
                CompanyName = "MesChain Tekstil",
                TaxNumber = "1234567890",
                Phone = "+90 541 510 16 15",
                Email = "info@meschain.com",
                Address = "Hacı Bayram, Anafartalar Cd. Ulus Şehir Çarşısı Kat:3 No:134, 06250 Altındağ/Ankara"
            });
            await SaveChangesAsync();
        }

        // 2) Warehouses
        var warehouses = new List<Warehouse>();
        if (!await Warehouses.AnyAsync())
        {
            warehouses = new List<Warehouse>
            {
                new Warehouse { Name = "Merkez Depo - Ulus", Code = "MRZ", IsActive = true, CreatedDate = DateTime.Now },
                new Warehouse { Name = "Üretim Depo - Sincan", Code = "URT", IsActive = true, CreatedDate = DateTime.Now },
                new Warehouse { Name = "Mağaza Depo - Kızılay", Code = "MGZ", IsActive = true, CreatedDate = DateTime.Now }
            };
            Warehouses.AddRange(warehouses);
            await SaveChangesAsync();
        }
        else
        {
            warehouses = await Warehouses.ToListAsync();
        }

        // 3) Categories (tekstil odaklı)
        var categories = new List<Category>();
        if (!await Categories.AnyAsync())
        {
            categories = new List<Category>
            {
                new Category { Name = "T-Shirt", Code = "TSHIRT", Description = "Pamuk tişörtler", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Name = "Pantolon", Code = "PANT", Description = "Kot/Keten pantolonlar", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Name = "Gömlek", Code = "GOMLEK", Description = "Klasik ve spor gömlek", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Name = "Mont", Code = "MONT", Description = "Dış giyim", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Name = "Ayakkabı", Code = "AYK", Description = "Spor ve günlük ayakkabı", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Name = "Aksesuar", Code = "AKSR", Description = "Kemer/çanta/şapka", IsActive = true, CreatedDate = DateTime.Now }
            };
            Categories.AddRange(categories);
            await SaveChangesAsync();
        }
        else
        {
            categories = await Categories.ToListAsync();
        }

        // 4) Customers (örnek)
        var customers = new List<Customer>();
        if (!await Customers.AnyAsync())
        {
            customers = new List<Customer>
            {
                new Customer { Name = "Ahmet Yılmaz", Phone = "+90 532 000 01 01", Email = "ahmet@example.com", IsActive = true, CreatedDate = DateTime.Now },
                new Customer { Name = "Fatma Kaya", Phone = "+90 532 000 02 02", Email = "fatma@example.com", IsActive = true, CreatedDate = DateTime.Now },
                new Customer { Name = "Mehmet Demir", Phone = "+90 532 000 03 03", Email = "mehmet@example.com", IsActive = true, CreatedDate = DateTime.Now },
                new Customer { Name = "Ayşe Şahin", Phone = "+90 532 000 04 04", Email = "ayse@example.com", IsActive = true, CreatedDate = DateTime.Now }
            };
            Customers.AddRange(customers);
            await SaveChangesAsync();
        }
        else
        {
            customers = await Customers.ToListAsync();
        }

        // 5) Products (30 adet, benzersiz SKU/Barcode)
        var random = new Random(20250811);
        var products = new List<Product>();
        if (!await Products.AnyAsync())
        {
            int skuIndex = 1000;
            long barcodeIndex = 8690000000000L; // Türkiye EAN-13 başlangıcı; örnek seri
            for (int i = 0; i < 30; i++)
            {
                var cat = categories[random.Next(categories.Count)];
                var wh = warehouses[random.Next(warehouses.Count)];
                var size = new[] { "S", "M", "L", "XL" }[random.Next(4)];
                var color = new[] { "Siyah", "Beyaz", "Lacivert", "Kırmızı", "Gri" }[random.Next(5)];
                var baseName = cat.Name switch
                {
                    "T-Shirt" => $"Basic T-Shirt {size} {color}",
                    "Pantolon" => $"Kot Pantolon {size} {color}",
                    "Gömlek" => $"Oxford Gömlek {size} {color}",
                    "Mont" => $"Kapitone Mont {size} {color}",
                    "Ayakkabı" => $"Spor Ayakkabı {size} {color}",
                    _ => $"Aksesuar {color}"
                };

                var purchase = (decimal)(random.Next(150, 1500));
                var sale = purchase * (decimal)(1.25 + random.NextDouble() * 0.6); // %25–%85 marj
                var stock = random.Next(0, 120);

                products.Add(new Product
                {
                    Name = baseName,
                    Description = cat.Description,
                    SKU = $"MS-{skuIndex + i}",
                    Barcode = (barcodeIndex + i).ToString(),
                    CategoryId = cat.Id,
                    WarehouseId = wh.Id,
                    PurchasePrice = Math.Round(purchase, 2),
                    SalePrice = Math.Round(sale, 2),
                    TaxRate = 18m,
                    Stock = stock,
                    MinimumStock = 5,
                    ReorderLevel = 12,
                    Location = $"{wh.Code}-{random.Next(1, 20)}.{random.Next(1, 10)}",
                    Color = color,
                    Size = size,
                    Brand = "MesChain",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-random.Next(0, 45))
                });
            }
            Products.AddRange(products);
            await SaveChangesAsync();
        }
        else
        {
            products = await Products.ToListAsync();
        }

        // 6) Orders & OrderItems (40 adet)
        if (!await Orders.AnyAsync())
        {
            int orderSeq = 20000;
            for (int i = 0; i < 40; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var orderDate = DateTime.Now.AddDays(-random.Next(0, 30));
                var status = (OrderStatus)random.Next(Enum.GetValues(typeof(OrderStatus)).Length);
                var order = new Order
                {
                    OrderNumber = $"MS-{DateTime.Now:yyyyMM}-{orderSeq + i}",
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    CustomerEmail = customer.Email,
                    Status = status,
                    OrderDate = orderDate,
                    TaxRate = 18m,
                    PaymentStatus = status == OrderStatus.Cancelled ? "CANCELLED" : (status == OrderStatus.Delivered ? "PAID" : "PENDING"),
                    CreatedAt = orderDate,
                    CreatedBy = "seed"
                };

                // 1-3 adet kalem
                int itemCount = random.Next(1, 4);
                var picked = products.OrderBy(_ => random.Next()).Take(itemCount).ToList();
                foreach (var p in picked)
                {
                    int qty = random.Next(1, 6);
                    var oi = new OrderItem
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        ProductSKU = p.SKU,
                        Quantity = qty,
                        UnitPrice = p.SalePrice,
                        TaxRate = 18m,
                        CreatedDate = orderDate
                    };
                    oi.CalculateAmounts();
                    order.OrderItems.Add(oi);
                }

                order.CalculateTotals();
                Orders.Add(order);
            }
            await SaveChangesAsync();
        }

        // 7) StockMovements (son 10 gün)
        if (!await StockMovements.AnyAsync())
        {
            var items = new List<StockMovement>();
            foreach (var p in products.OrderBy(_ => random.Next()).Take(25))
            {
                var movement = new StockMovement
                {
                    ProductId = p.Id,
                    Quantity = random.Next(1, 15),
                    MovementType = "IN",
                    Notes = "Başlangıç stok girişi",
                    Date = DateTime.Now.AddDays(-random.Next(0, 10))
                };
                items.Add(movement);
            }
            StockMovements.AddRange(items);
            await SaveChangesAsync();
        }
    }

    /// <summary>
    /// Çalışma zamanında kritik indeksleri kontrol eder ve yoksa oluşturur (SQL Server).
    /// Mevcut veritabanını migration çalıştırmadan PRD indeksleri ile hizalamak için kullanılır.
    /// </summary>
    public async Task EnsureIndexesCreatedAsync()
    {
        var sqlCommands = new[]
        {
            // Products
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_SKU' AND object_id = OBJECT_ID('[dbo].[Products]'))\nCREATE UNIQUE INDEX [IX_Products_SKU] ON [dbo].[Products] ([SKU]);",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Barcode' AND object_id = OBJECT_ID('[dbo].[Products]'))\nCREATE UNIQUE INDEX [IX_Products_Barcode] ON [dbo].[Products] ([Barcode]);",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Name_IsActive' AND object_id = OBJECT_ID('[dbo].[Products]'))\nCREATE INDEX [IX_Products_Name_IsActive] ON [dbo].[Products] ([Name],[IsActive]);",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Category_IsActive' AND object_id = OBJECT_ID('[dbo].[Products]'))\nCREATE INDEX [IX_Products_Category_IsActive] ON [dbo].[Products] ([CategoryId],[IsActive]);",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Warehouse_IsActive' AND object_id = OBJECT_ID('[dbo].[Products]'))\nCREATE INDEX [IX_Products_Warehouse_IsActive] ON [dbo].[Products] ([WarehouseId],[IsActive]);",

            // Categories
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Categories_Code' AND object_id = OBJECT_ID('[dbo].[Categories]'))\nCREATE UNIQUE INDEX [IX_Categories_Code] ON [dbo].[Categories] ([Code]);",

            // Orders
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_OrderNumber' AND object_id = OBJECT_ID('[dbo].[Orders]'))\nCREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [dbo].[Orders] ([OrderNumber]);",

            // StockMovements
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_Product_Date' AND object_id = OBJECT_ID('[dbo].[StockMovements]'))\nCREATE INDEX [IX_StockMovements_Product_Date] ON [dbo].[StockMovements] ([ProductId],[Date]);"
        };

        foreach (var sql in sqlCommands)
        {
            try
            {
                await Database.ExecuteSqlRawAsync(sql);
            }
            catch
            {
                // Indeks var veya izin yok vb. durumlarda uygulamayı durdurmayalım
            }
        }
    }

    /// <summary>
    /// Değişiklikleri kaydetmeden önce timestamps güncellenir
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Async version of SaveChanges with timestamp updates
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// UpdatedAt alanlarını otomatik günceller
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // CreatedDate property'si varsa güncelle
            var createdDateProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedDate");
            if (entry.State == EntityState.Added && createdDateProperty != null)
            {
                if (createdDateProperty.CurrentValue == null)
                {
                    createdDateProperty.CurrentValue = DateTime.Now;
                }
            }

            // ModifiedDate property'si varsa güncelle
            var modifiedDateProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ModifiedDate");
            if (entry.State == EntityState.Modified && modifiedDateProperty != null)
            {
                modifiedDateProperty.CurrentValue = DateTime.Now;
            }
        }
    }

    /// <summary>
    /// Telemetry tablolarını (örn. ApiCallLogs) mevcut veritabanına non-destructive şekilde ekler.
    /// Migration eksikliği durumunda canlı sistemde kesinti olmadan tabloyu oluşturmak için kullanılır.
    /// Not: Uzun vadede resmi EF Migration (AddApiCallLog) oluşturulmalıdır.
    /// </summary>
    public async Task EnsureTelemetryTablesCreatedAsync()
    {
        // ONLY SQL SERVER: OBJECT_ID kontrolü ile tablo yoksa oluştur
        const string createApiCallLogTable = @"IF OBJECT_ID('dbo.ApiCallLogs','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApiCallLogs](
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Endpoint] NVARCHAR(256) NOT NULL,
        [Method] NVARCHAR(10) NOT NULL,
        [Success] BIT NOT NULL,
        [StatusCode] INT NULL,
        [Category] NVARCHAR(32) NULL,
        [DurationMs] BIGINT NOT NULL,
        [TimestampUtc] DATETIME2 NOT NULL,
        [CorrelationId] NVARCHAR(64) NULL
    );
    CREATE INDEX IX_ApiCallLogs_TimestampUtc ON [dbo].[ApiCallLogs]([TimestampUtc]);
    CREATE INDEX IX_ApiCallLogs_Endpoint_TimestampUtc ON [dbo].[ApiCallLogs]([Endpoint],[TimestampUtc]);
END";

        const string createCircuitStateLogTable = @"IF OBJECT_ID('dbo.CircuitStateLogs','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CircuitStateLogs](
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PreviousState] NVARCHAR(20) NOT NULL,
        [NewState] NVARCHAR(20) NOT NULL,
        [Reason] NVARCHAR(100) NOT NULL,
        [FailureRate] FLOAT NOT NULL,
        [WindowTotalCalls] INT NOT NULL,
        [TransitionTimeUtc] DATETIME2 NOT NULL,
        [CorrelationId] NVARCHAR(64) NULL,
        [AdditionalInfo] NVARCHAR(256) NULL
    );
    CREATE INDEX IX_CircuitStateLogs_TransitionTimeUtc ON [dbo].[CircuitStateLogs]([TransitionTimeUtc]);
    CREATE INDEX IX_CircuitStateLogs_NewState_TransitionTimeUtc ON [dbo].[CircuitStateLogs]([NewState],[TransitionTimeUtc]);
END";

        const string createBarcodeScanLogTable = @"IF OBJECT_ID('dbo.BarcodeScanLogs','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BarcodeScanLogs](
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Barcode] NVARCHAR(256) NOT NULL,
        [Format] NVARCHAR(32) NOT NULL,
        [Source] NVARCHAR(16) NOT NULL,
        [DeviceId] NVARCHAR(64) NULL,
        [IsValid] BIT NOT NULL,
        [ValidationMessage] NVARCHAR(256) NULL,
        [RawLength] INT NOT NULL,
        [TimestampUtc] DATETIME2 NOT NULL,
        [CorrelationId] NVARCHAR(64) NULL
    );
    CREATE INDEX IX_BarcodeScanLogs_TimestampUtc ON [dbo].[BarcodeScanLogs]([TimestampUtc]);
    CREATE INDEX IX_BarcodeScanLogs_Format_TimestampUtc ON [dbo].[BarcodeScanLogs]([Format],[TimestampUtc]);
END";

        try
        {
            await Database.ExecuteSqlRawAsync(createApiCallLogTable);
            await Database.ExecuteSqlRawAsync(createCircuitStateLogTable);
            await Database.ExecuteSqlRawAsync(createBarcodeScanLogTable);
        }
        catch
        {
            // Tablo oluşturma başarısız ise (izin / race) uygulamayı durdurma
        }
    }

    /// <summary>
    /// Canlı sistemde migration gerektirmeden concurrency (RowVersion) ve senkronizasyon alanlarını (SyncedAt, IdempotencyKey, ExternalId) güvenli şekilde ekler.
    /// Yoksa eklenir, varsa atlanır.
    /// </summary>
    public async Task EnsureConcurrencyAndSyncColumnsCreatedAsync()
    {
        // Products
        const string addProdRowVersion = "IF COL_LENGTH('dbo.Products','RowVersion') IS NULL ALTER TABLE [dbo].[Products] ADD [RowVersion] rowversion;";
        const string addProdSyncedAt = "IF COL_LENGTH('dbo.Products','SyncedAt') IS NULL ALTER TABLE [dbo].[Products] ADD [SyncedAt] DATETIME2(7) NULL;";

        // Orders
        const string addOrderRowVersion = "IF COL_LENGTH('dbo.Orders','RowVersion') IS NULL ALTER TABLE [dbo].[Orders] ADD [RowVersion] rowversion;";
        const string addOrderSyncedAt = "IF COL_LENGTH('dbo.Orders','SyncedAt') IS NULL ALTER TABLE [dbo].[Orders] ADD [SyncedAt] DATETIME2(7) NULL;";
        const string addOrderExternalId = "IF COL_LENGTH('dbo.Orders','ExternalId') IS NULL ALTER TABLE [dbo].[Orders] ADD [ExternalId] NVARCHAR(100) NULL;";
        const string addOrderIdemKey = "IF COL_LENGTH('dbo.Orders','IdempotencyKey') IS NULL ALTER TABLE [dbo].[Orders] ADD [IdempotencyKey] NVARCHAR(100) NULL;";

        // Customers
        const string addCustRowVersion = "IF COL_LENGTH('dbo.Customers','RowVersion') IS NULL ALTER TABLE [dbo].[Customers] ADD [RowVersion] rowversion;";
        const string addCustSyncedAt = "IF COL_LENGTH('dbo.Customers','SyncedAt') IS NULL ALTER TABLE [dbo].[Customers] ADD [SyncedAt] DATETIME2(7) NULL;";

        // StockMovements
        const string addMovRowVersion = "IF COL_LENGTH('dbo.StockMovements','RowVersion') IS NULL ALTER TABLE [dbo].[StockMovements] ADD [RowVersion] rowversion;";
        const string addMovIdemKey = "IF COL_LENGTH('dbo.StockMovements','IdempotencyKey') IS NULL ALTER TABLE [dbo].[StockMovements] ADD [IdempotencyKey] NVARCHAR(100) NULL;";

        var commands = new[]
        {
            addProdRowVersion, addProdSyncedAt,
            addOrderRowVersion, addOrderSyncedAt, addOrderExternalId, addOrderIdemKey,
            addCustRowVersion, addCustSyncedAt,
            addMovRowVersion, addMovIdemKey
        };

        foreach (var sql in commands)
        {
            try { await Database.ExecuteSqlRawAsync(sql); } catch { }
        }
    }

    /// <summary>
    /// Üretim kritik ek indeksleri (filtered unique dahil) güvenli şekilde oluşturur.
    /// Yoksa eklenir, varsa atlanır.
    /// </summary>
    public async Task EnsureProductionIndexesExtendedAsync()
    {
        var commands = new[]
        {
            // Customers: Unique Email (WHERE NOT NULL)
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Email_NotNull' AND object_id = OBJECT_ID('[dbo].[Customers]'))\nCREATE UNIQUE INDEX [IX_Customers_Email_NotNull] ON [dbo].[Customers] ([Email]) WHERE [Email] IS NOT NULL;",
            // Customers: Unique Phone (WHERE NOT NULL)
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Phone_NotNull' AND object_id = OBJECT_ID('[dbo].[Customers]'))\nCREATE UNIQUE INDEX [IX_Customers_Phone_NotNull] ON [dbo].[Customers] ([Phone]) WHERE [Phone] IS NOT NULL;",
            // Orders: ExternalId + SyncedAt (kolonlar yoksa EnsureConcurrencyAndSyncColumnsCreatedAsync ekler)
            "IF COL_LENGTH('dbo.Orders','ExternalId') IS NOT NULL AND COL_LENGTH('dbo.Orders','SyncedAt') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_ExternalId_SyncedAt' AND object_id = OBJECT_ID('[dbo].[Orders]'))\nCREATE INDEX [IX_Orders_ExternalId_SyncedAt] ON [dbo].[Orders] ([ExternalId],[SyncedAt]);",
            // StockMovements: Date, MovementType, ProductId (raporlama için)
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_Date_Type_ProductId' AND object_id = OBJECT_ID('[dbo].[StockMovements]'))\nCREATE INDEX [IX_StockMovements_Date_Type_ProductId] ON [dbo].[StockMovements] ([Date],[MovementType],[ProductId]);"
        };

        foreach (var sql in commands)
        {
            try { await Database.ExecuteSqlRawAsync(sql); } catch { }
        }
    }

    /// <summary>
    /// Products tablosuna denetim/regülasyon alanlarını ekler (UsageInstructions, ImporterInfo, ManufacturerInfo) – yoksa
    /// </summary>
    public async Task EnsureProductRegulatoryColumnsCreatedAsync()
    {
        const string addUsage = "IF COL_LENGTH('dbo.Products','UsageInstructions') IS NULL ALTER TABLE [dbo].[Products] ADD [UsageInstructions] NVARCHAR(1000) NULL;";
        const string addImporter = "IF COL_LENGTH('dbo.Products','ImporterInfo') IS NULL ALTER TABLE [dbo].[Products] ADD [ImporterInfo] NVARCHAR(255) NULL;";
        const string addManufacturer = "IF COL_LENGTH('dbo.Products','ManufacturerInfo') IS NULL ALTER TABLE [dbo].[Products] ADD [ManufacturerInfo] NVARCHAR(255) NULL;";
        try { await Database.ExecuteSqlRawAsync(addUsage); } catch { }
        try { await Database.ExecuteSqlRawAsync(addImporter); } catch { }
        try { await Database.ExecuteSqlRawAsync(addManufacturer); } catch { }
    }

    /// <summary>
    /// Products tablosuna ticari/lojistik alanları ekler (Origin, Material, VolumeText, Desi, LeadTimeDays, Ship/ReturnAddress, Sizes, DiscountRate) – yoksa
    /// </summary>
    public async Task EnsureProductExtendedColumnsCreatedAsync()
    {
        const string addOrigin = "IF COL_LENGTH('dbo.Products','Origin') IS NULL ALTER TABLE [dbo].[Products] ADD [Origin] NVARCHAR(50) NULL;";
        const string addMaterial = "IF COL_LENGTH('dbo.Products','Material') IS NULL ALTER TABLE [dbo].[Products] ADD [Material] NVARCHAR(50) NULL;";
        const string addVolumeText = "IF COL_LENGTH('dbo.Products','VolumeText') IS NULL ALTER TABLE [dbo].[Products] ADD [VolumeText] NVARCHAR(50) NULL;";
        const string addDesi = "IF COL_LENGTH('dbo.Products','Desi') IS NULL ALTER TABLE [dbo].[Products] ADD [Desi] DECIMAL(10,2) NULL;";
        const string addLeadTimeDays = "IF COL_LENGTH('dbo.Products','LeadTimeDays') IS NULL ALTER TABLE [dbo].[Products] ADD [LeadTimeDays] INT NULL;";
        const string addShipAddress = "IF COL_LENGTH('dbo.Products','ShipAddress') IS NULL ALTER TABLE [dbo].[Products] ADD [ShipAddress] NVARCHAR(255) NULL;";
        const string addReturnAddress = "IF COL_LENGTH('dbo.Products','ReturnAddress') IS NULL ALTER TABLE [dbo].[Products] ADD [ReturnAddress] NVARCHAR(255) NULL;";
        const string addSizes = "IF COL_LENGTH('dbo.Products','Sizes') IS NULL ALTER TABLE [dbo].[Products] ADD [Sizes] NVARCHAR(50) NULL;";
        const string addDiscountRate = "IF COL_LENGTH('dbo.Products','DiscountRate') IS NULL ALTER TABLE [dbo].[Products] ADD [DiscountRate] DECIMAL(5,2) NULL;";

        var commands = new[]
        {
            addOrigin, addMaterial, addVolumeText, addDesi, addLeadTimeDays,
            addShipAddress, addReturnAddress, addSizes, addDiscountRate
        };
        foreach (var sql in commands)
        {
            try { await Database.ExecuteSqlRawAsync(sql); } catch { }
        }
    }

    /// <summary>
    /// Products tablosundaki TÜM yeni alanları (modelde var olup tabloda olmayanları) güvenli şekilde ekler.
    /// CANLI ortamda migration olmadan hataları engellemek için kullanılır.
    /// </summary>
    public async Task EnsureProductAllColumnsCreatedAsync()
    {
        var commands = new[]
        {
            // GS1 ve fiyat
            "IF COL_LENGTH('dbo.Products','GTIN') IS NULL ALTER TABLE [dbo].[Products] ADD [GTIN] NVARCHAR(14) NULL;",
            "IF COL_LENGTH('dbo.Products','UPC') IS NULL ALTER TABLE [dbo].[Products] ADD [UPC] NVARCHAR(20) NULL;",
            "IF COL_LENGTH('dbo.Products','EAN') IS NULL ALTER TABLE [dbo].[Products] ADD [EAN] NVARCHAR(20) NULL;",
            "IF COL_LENGTH('dbo.Products','ListPrice') IS NULL ALTER TABLE [dbo].[Products] ADD [ListPrice] DECIMAL(18,2) NULL;",
            "IF COL_LENGTH('dbo.Products','TaxRate') IS NULL ALTER TABLE [dbo].[Products] ADD [TaxRate] DECIMAL(5,2) NOT NULL CONSTRAINT DF_Products_TaxRate DEFAULT(18);",

            // Stok eşikleri
            "IF COL_LENGTH('dbo.Products','MaximumStock') IS NULL ALTER TABLE [dbo].[Products] ADD [MaximumStock] INT NULL;",
            "IF COL_LENGTH('dbo.Products','ReorderLevel') IS NULL ALTER TABLE [dbo].[Products] ADD [ReorderLevel] INT NULL;",
            "IF COL_LENGTH('dbo.Products','ReorderQuantity') IS NULL ALTER TABLE [dbo].[Products] ADD [ReorderQuantity] INT NULL;",

            // İlişkisel opsiyoneller
            "IF COL_LENGTH('dbo.Products','SupplierId') IS NULL ALTER TABLE [dbo].[Products] ADD [SupplierId] INT NULL;",
            "IF COL_LENGTH('dbo.Products','WarehouseId') IS NULL ALTER TABLE [dbo].[Products] ADD [WarehouseId] INT NULL;",

            // Fiziksel özellikler
            "IF COL_LENGTH('dbo.Products','Weight') IS NULL ALTER TABLE [dbo].[Products] ADD [Weight] DECIMAL(10,3) NULL;",
            "IF COL_LENGTH('dbo.Products','Length') IS NULL ALTER TABLE [dbo].[Products] ADD [Length] DECIMAL(10,2) NULL;",
            "IF COL_LENGTH('dbo.Products','Width') IS NULL ALTER TABLE [dbo].[Products] ADD [Width] DECIMAL(10,2) NULL;",
            "IF COL_LENGTH('dbo.Products','Height') IS NULL ALTER TABLE [dbo].[Products] ADD [Height] DECIMAL(10,2) NULL;",
            "IF COL_LENGTH('dbo.Products','WeightUnit') IS NULL ALTER TABLE [dbo].[Products] ADD [WeightUnit] NVARCHAR(10) NULL;",
            "IF COL_LENGTH('dbo.Products','DimensionUnit') IS NULL ALTER TABLE [dbo].[Products] ADD [DimensionUnit] NVARCHAR(10) NULL;",

            // Lokasyon
            "IF COL_LENGTH('dbo.Products','Shelf') IS NULL ALTER TABLE [dbo].[Products] ADD [Shelf] NVARCHAR(20) NULL;",
            "IF COL_LENGTH('dbo.Products','Bin') IS NULL ALTER TABLE [dbo].[Products] ADD [Bin] NVARCHAR(20) NULL;",

            // Bayraklar
            "IF COL_LENGTH('dbo.Products','IsDiscontinued') IS NULL ALTER TABLE [dbo].[Products] ADD [IsDiscontinued] BIT NOT NULL CONSTRAINT DF_Products_IsDiscontinued DEFAULT(0);",
            "IF COL_LENGTH('dbo.Products','IsSerialized') IS NULL ALTER TABLE [dbo].[Products] ADD [IsSerialized] BIT NOT NULL CONSTRAINT DF_Products_IsSerialized DEFAULT(0);",
            "IF COL_LENGTH('dbo.Products','IsBatchTracked') IS NULL ALTER TABLE [dbo].[Products] ADD [IsBatchTracked] BIT NOT NULL CONSTRAINT DF_Products_IsBatchTracked DEFAULT(0);",
            "IF COL_LENGTH('dbo.Products','IsPerishable') IS NULL ALTER TABLE [dbo].[Products] ADD [IsPerishable] BIT NOT NULL CONSTRAINT DF_Products_IsPerishable DEFAULT(0);",

            // Tarihler
            "IF COL_LENGTH('dbo.Products','ExpiryDate') IS NULL ALTER TABLE [dbo].[Products] ADD [ExpiryDate] DATETIME2(7) NULL;",
            "IF COL_LENGTH('dbo.Products','LastStockUpdate') IS NULL ALTER TABLE [dbo].[Products] ADD [LastStockUpdate] DATETIME2(7) NULL;",

            // Kullanıcı izleme
            "IF COL_LENGTH('dbo.Products','CreatedBy') IS NULL ALTER TABLE [dbo].[Products] ADD [CreatedBy] NVARCHAR(50) NULL;",
            "IF COL_LENGTH('dbo.Products','ModifiedBy') IS NULL ALTER TABLE [dbo].[Products] ADD [ModifiedBy] NVARCHAR(50) NULL;",

            // Dosyalar/Görseller
            "IF COL_LENGTH('dbo.Products','ImageUrls') IS NULL ALTER TABLE [dbo].[Products] ADD [ImageUrls] NVARCHAR(500) NULL;",
            "IF COL_LENGTH('dbo.Products','DocumentUrls') IS NULL ALTER TABLE [dbo].[Products] ADD [DocumentUrls] NVARCHAR(500) NULL;",

            // Marka/Model
            "IF COL_LENGTH('dbo.Products','Model') IS NULL ALTER TABLE [dbo].[Products] ADD [Model] NVARCHAR(50) NULL;",

            // Tekli beden alanı (mevcut şema ile uyumluluk)
            "IF COL_LENGTH('dbo.Products','Size') IS NULL ALTER TABLE [dbo].[Products] ADD [Size] NVARCHAR(20) NULL;",

            // Not/Etiketler
            "IF COL_LENGTH('dbo.Products','Notes') IS NULL ALTER TABLE [dbo].[Products] ADD [Notes] NVARCHAR(1000) NULL;",
            "IF COL_LENGTH('dbo.Products','Tags') IS NULL ALTER TABLE [dbo].[Products] ADD [Tags] NVARCHAR(200) NULL;",

            // OpenCart entegrasyon
            "IF COL_LENGTH('dbo.Products','OpenCartProductId') IS NULL ALTER TABLE [dbo].[Products] ADD [OpenCartProductId] INT NULL;",
            "IF COL_LENGTH('dbo.Products','LastSyncDate') IS NULL ALTER TABLE [dbo].[Products] ADD [LastSyncDate] DATETIME2(7) NULL;",
            "IF COL_LENGTH('dbo.Products','LastModifiedAt') IS NULL ALTER TABLE [dbo].[Products] ADD [LastModifiedAt] DATETIME2(7) NULL;",
            "IF COL_LENGTH('dbo.Products','SyncWithOpenCart') IS NULL ALTER TABLE [dbo].[Products] ADD [SyncWithOpenCart] BIT NOT NULL CONSTRAINT DF_Products_SyncWithOpenCart DEFAULT(1);"
        };

        foreach (var sql in commands)
        {
            try { await Database.ExecuteSqlRawAsync(sql); } catch { }
        }
    }

    /// <summary>
    /// AI Configuration Models - A++++ Enterprise Integration
    /// AI API konfigürasyonları ve kullanım logları için model tanımlamaları
    /// </summary>
    private void ConfigureAIModels(ModelBuilder modelBuilder)
    {
        // AIConfiguration Entity
        modelBuilder.Entity<MesTechStok.Core.Data.Models.AIConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProviderName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ApiKey).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ApiEndpoint).HasMaxLength(200);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.SystemPrompt).HasColumnType("nvarchar(max)");
            entity.Property(e => e.EncryptionKey).HasMaxLength(100);
            entity.Property(e => e.LastErrorMessage).HasMaxLength(500);
            entity.Property(e => e.ProviderSettings).HasColumnType("nvarchar(max)");

            // Indexes for performance
            entity.HasIndex(e => e.ProviderName);
            entity.HasIndex(e => new { e.IsActive, e.IsDefault });
            entity.HasIndex(e => e.LastUsedDate);

            // Constraints
            entity.Property(e => e.MaxRequestsPerMinute).HasDefaultValue(60);
            entity.Property(e => e.MaxRequestsPerDay).HasDefaultValue(1000);
            entity.Property(e => e.Temperature).HasDefaultValue(0.7);
            entity.Property(e => e.MaxTokens).HasDefaultValue(1000);
            entity.Property(e => e.TimeoutSeconds).HasDefaultValue(30);
        });

        // AIUsageLog Entity
        modelBuilder.Entity<MesTechStok.Core.Data.Models.AIUsageLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.RequestData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ResponseData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);

            // Foreign Key Relationship
            entity.HasOne(l => l.AIConfiguration)
                  .WithMany()
                  .HasForeignKey(l => l.AIConfigurationId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance and analytics
            entity.HasIndex(e => e.RequestDate);
            entity.HasIndex(e => new { e.AIConfigurationId, e.RequestDate });
            entity.HasIndex(e => new { e.RequestType, e.RequestDate });
            entity.HasIndex(e => new { e.IsSuccessful, e.RequestDate });
            entity.HasIndex(e => e.UserId);
        });
    }

    /// <summary>
    /// Stok Yerleşim Sistemi modellerinin konfigürasyonu - TEMPORARILY DISABLED
    /// İlişkiler, indeksler ve kısıtlar tanımlanır
    /// </summary>
    private void ConfigureLocationModels(ModelBuilder modelBuilder)
    {
        // EMERGENCY FIX: Method intentionally left empty
        // All warehouse location entities configuration disabled to prevent build errors
    }
}  // AppDbContext class
