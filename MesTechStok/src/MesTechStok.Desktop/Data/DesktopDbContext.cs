using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MesTechStok.Desktop.Data
{
    // Simplified models for Desktop app
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ListPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 0.18m;

        public int Stock { get; set; }
        public int MinimumStock { get; set; } = 5;
        public int MaximumStock { get; set; } = 1000;
        public int ReorderLevel { get; set; } = 10;
        public int ReorderQuantity { get; set; } = 50;

        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;

        public int? SupplierId { get; set; }
        public int? TaxRateId { get; set; }

        // GS1 Standards
        [MaxLength(14)]
        public string? GTIN { get; set; }

        [MaxLength(20)]
        public string? UPC { get; set; }

        [MaxLength(20)]
        public string? EAN { get; set; }

        // Physical Properties
        [Column(TypeName = "decimal(10,3)")]
        public decimal? Weight { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Length { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Height { get; set; }

        [MaxLength(10)]
        public string? WeightUnit { get; set; } = "kg";

        [MaxLength(10)]
        public string? DimensionUnit { get; set; } = "cm";

        // Location & Organization
        [MaxLength(50)]
        public string? Location { get; set; }

        [MaxLength(20)]
        public string? Shelf { get; set; }

        [MaxLength(20)]
        public string? Bin { get; set; }

        public int? WarehouseId { get; set; }

        // Status & Flags
        public bool IsDiscontinued { get; set; } = false;
        public bool IsSerialized { get; set; } = false;
        public bool IsBatchTracked { get; set; } = false;
        public bool IsPerishable { get; set; } = false;

        // Dates
        public DateTime? ExpiryDate { get; set; }
        public DateTime? LastStockUpdate { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        [MaxLength(100)]
        public string? LastUpdatedBy { get; set; }

        public DateTime? SyncedAt { get; set; }

        [MaxLength(50)]
        public string? Brand { get; set; }

        [MaxLength(50)]
        public string? Model { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(20)]
        public string? Size { get; set; }

        [MaxLength(50)]
        public string? Sizes { get; set; }

        // Additional Properties
        [MaxLength(50)]
        public string? Origin { get; set; }

        [MaxLength(50)]
        public string? Material { get; set; }

        [MaxLength(50)]
        public string? VolumeText { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Desi { get; set; }

        public int? LeadTimeDays { get; set; }

        [MaxLength(255)]
        public string? ShipAddress { get; set; }

        [MaxLength(255)]
        public string? ReturnAddress { get; set; }

        [MaxLength(1000)]
        public string? UsageInstructions { get; set; }

        [MaxLength(255)]
        public string? ImporterInfo { get; set; }

        [MaxLength(255)]
        public string? ManufacturerInfo { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(200)]
        public string? Tags { get; set; }

        [MaxLength(255)]
        public string? Icon { get; set; }

        [MaxLength(500)]
        public string? ImageUrls { get; set; }

        [MaxLength(500)]
        public string? DocumentUrls { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountRate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        [MaxLength(50)]
        public string? ModifiedBy { get; set; }

        // OpenCart Integration fields
        [MaxLength(50)]
        public string? Code { get; set; }

        public int? OpenCartProductId { get; set; }
        public int? OpenCartCategoryId { get; set; }
        public int? ParentCategoryId { get; set; }

        public bool ShowInMenu { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        public DateTime? LastSyncDate { get; set; }
        public bool SyncWithOpenCart { get; set; } = true;

        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        // Navigation Properties
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        // Calculated Properties
        [NotMapped]
        public decimal ProfitMargin => SalePrice > 0 ? ((SalePrice - PurchasePrice) / SalePrice) * 100 : 0;

        [NotMapped]
        public decimal TotalValue => Stock * PurchasePrice;

        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (Stock == 0) return "Tükendi";
                if (Stock <= MinimumStock) return "Düşük Stok";
                if (Stock <= ReorderLevel) return "Yeniden Sipariş";
                return "Yeterli";
            }
        }

        [NotMapped]
        public string StockStatusColor
        {
            get
            {
                return StockStatus switch
                {
                    "Tükendi" => "#F44336",
                    "Düşük Stok" => "#FF9800",
                    "Yeniden Sipariş" => "#2196F3",
                    _ => "#4CAF50"
                };
            }
        }
    }

    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public int? ParentCategoryId { get; set; }
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

        [MaxLength(7)]
        public string? Color { get; set; } = "#2196F3";

        [MaxLength(50)]
        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public int Quantity { get; set; }
        public int NewStockLevel { get; set; }

        [Required]
        [MaxLength(50)]
        public string MovementType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT

        [MaxLength(200)]
        public string? Reason { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? ProcessedBy { get; set; }

        [MaxLength(50)]
        public string? ScannedBarcode { get; set; }

        public bool IsScannedMovement { get; set; } = false;
    }

    public class DesktopDbContext : DbContext
    {
        private readonly string _connectionString;

        public DesktopDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DesktopDbContext(DbContextOptions<DesktopDbContext> options) : base(options)
        {
            _connectionString = "";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Bu yardımcı context yalnızca demo/test amaçlıydı.
                // Tek kaynaklı mimaride kullanılmamalı; varsayılanı SQL Server yap.
                if (!string.IsNullOrWhiteSpace(_connectionString))
                {
                    optionsBuilder.UseSqlServer(_connectionString);
                }
            }
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product Configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SKU).IsUnique();
                entity.HasIndex(e => e.Barcode).IsUnique();
                entity.HasIndex(e => new { e.Name, e.IsActive });

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Category Configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Code).IsUnique();

                entity.HasOne(e => e.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(e => e.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StockMovement Configuration
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ProductId, e.Date });
                entity.HasIndex(e => e.MovementType);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.StockMovements)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed Data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Elektronik",
                    Code = "ELEK",
                    Description = "Elektronik ürünler",
                    Color = "#2196F3",
                    Icon = "📱",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Category
                {
                    Id = 2,
                    Name = "Telefon",
                    Code = "TEL",
                    Description = "Akıllı telefonlar",
                    ParentCategoryId = 1,
                    Color = "#4CAF50",
                    Icon = "📱",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Category
                {
                    Id = 3,
                    Name = "Bilgisayar",
                    Code = "PC",
                    Description = "Bilgisayar ve aksesuarları",
                    ParentCategoryId = 1,
                    Color = "#FF9800",
                    Icon = "💻",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Category
                {
                    Id = 4,
                    Name = "Aksesuarlar",
                    Code = "AKS",
                    Description = "Elektronik aksesuarları",
                    ParentCategoryId = 1,
                    Color = "#9C27B0",
                    Icon = "🎧",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );

            // Seed Products
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Samsung Galaxy S23",
                    SKU = "SAM-GS23-128",
                    Barcode = "1234567890123",
                    Description = "Samsung Galaxy S23 128GB Akıllı Telefon",
                    CategoryId = 2,
                    PurchasePrice = 20000m,
                    SalePrice = 25000m,
                    TaxRate = 0.18m,
                    Stock = 45,
                    MinimumStock = 5,
                    ReorderLevel = 10,
                    Brand = "Samsung",
                    Model = "Galaxy S23",
                    Color = "Siyah",
                    Size = "128GB",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Product
                {
                    Id = 2,
                    Name = "iPhone 15 Pro",
                    SKU = "APL-IP15P-256",
                    Barcode = "2345678901234",
                    Description = "Apple iPhone 15 Pro 256GB",
                    CategoryId = 2,
                    PurchasePrice = 30000m,
                    SalePrice = 35000m,
                    TaxRate = 0.18m,
                    Stock = 32,
                    MinimumStock = 3,
                    ReorderLevel = 8,
                    Brand = "Apple",
                    Model = "iPhone 15 Pro",
                    Color = "Titanium",
                    Size = "256GB",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Product
                {
                    Id = 3,
                    Name = "MacBook Air M2",
                    SKU = "APL-MBA-M2-512",
                    Barcode = "3456789012345",
                    Description = "Apple MacBook Air M2 512GB",
                    CategoryId = 3,
                    PurchasePrice = 25000m,
                    SalePrice = 28000m,
                    TaxRate = 0.18m,
                    Stock = 18,
                    MinimumStock = 2,
                    ReorderLevel = 5,
                    Brand = "Apple",
                    Model = "MacBook Air",
                    Color = "Space Gray",
                    Size = "13.6\"",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Product
                {
                    Id = 4,
                    Name = "Dell XPS 13",
                    SKU = "DEL-XPS13-1TB",
                    Barcode = "4567890123456",
                    Description = "Dell XPS 13 1TB SSD Ultrabook",
                    CategoryId = 3,
                    PurchasePrice = 18000m,
                    SalePrice = 22000m,
                    TaxRate = 0.18m,
                    Stock = 25,
                    MinimumStock = 3,
                    ReorderLevel = 7,
                    Brand = "Dell",
                    Model = "XPS 13",
                    Color = "Silver",
                    Size = "13.4\"",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Product
                {
                    Id = 5,
                    Name = "Sony WH-1000XM5",
                    SKU = "SON-WH1000XM5",
                    Barcode = "5678901234567",
                    Description = "Sony WH-1000XM5 Noise Cancelling Kulaklık",
                    CategoryId = 4,
                    PurchasePrice = 7000m,
                    SalePrice = 8500m,
                    TaxRate = 0.18m,
                    Stock = 67,
                    MinimumStock = 10,
                    ReorderLevel = 20,
                    Brand = "Sony",
                    Model = "WH-1000XM5",
                    Color = "Siyah",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                }
            );
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Property("ModifiedDate") != null)
                {
                    entry.Property("ModifiedDate").CurrentValue = DateTime.Now;
                }
            }
        }
    }
}