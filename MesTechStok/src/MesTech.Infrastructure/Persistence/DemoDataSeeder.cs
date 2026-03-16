using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Persistence;

/// <summary>
/// Demo tenant seed data — ilk calistirmada bos DB'yi anlamli demo verisiyle doldurur.
/// Mevcut DataSeeder (Default Tenant/Store) calistiktan sonra cagrilir.
/// Idempotent: DemoTenantId zaten varsa atlar.
/// </summary>
public class DemoDataSeeder
{
    public static readonly Guid DemoTenantId =
        Guid.Parse("00000000-0000-0000-0000-000000000099");

    private static readonly Guid DemoStoreId =
        Guid.Parse("00000000-0000-0000-0000-000000000098");

    private static readonly Guid DemoCategoryId =
        Guid.Parse("00000000-0000-0000-0000-000000000097");

    private static readonly Guid DemoCustomerId =
        Guid.Parse("00000000-0000-0000-0000-000000000096");

    private readonly AppDbContext _context;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(AppDbContext context, ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var exists = await _context.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == DemoTenantId, ct);

        if (exists)
        {
            _logger.LogInformation("Demo tenant zaten mevcut, seed atlandi");
            return;
        }

        _logger.LogInformation("Demo verileri olusturuluyor...");

        await SeedDemoTenantAsync(ct);
        await SeedDemoStoreAsync(ct);
        await SeedDemoCategoryAsync(ct);
        await SeedDemoCustomerAsync(ct);

        var productIds = await SeedDemoProductsAsync(ct);
        await SeedDemoOrdersAsync(productIds, ct);

        _logger.LogInformation("Demo verileri basariyla olusturuldu: 1 tenant, 1 store, 1 category, 1 customer, {ProductCount} product, 5 order",
            productIds.Count);
    }

    private static void SetEntityId(object entity, Guid id)
    {
        typeof(Domain.Common.BaseEntity)
            .GetProperty(nameof(Domain.Common.BaseEntity.Id))!
            .SetValue(entity, id);
    }

    private async Task SeedDemoTenantAsync(CancellationToken ct)
    {
        var tenant = new Tenant
        {
            Name = "Demo Sirket",
            TaxNumber = "1234567890",
            IsActive = true,
            CreatedBy = "DemoDataSeeder"
        };
        SetEntityId(tenant, DemoTenantId);

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Demo Tenant olusturuldu: {TenantId}", DemoTenantId);
    }

    private async Task SeedDemoStoreAsync(CancellationToken ct)
    {
        var store = new Store
        {
            TenantId = DemoTenantId,
            StoreName = "Demo Trendyol Magazasi",
            PlatformType = PlatformType.Trendyol,
            IsActive = true,
            CreatedBy = "DemoDataSeeder"
        };
        SetEntityId(store, DemoStoreId);

        _context.Stores.Add(store);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Demo Store olusturuldu: {StoreId}", DemoStoreId);
    }

    private async Task SeedDemoCategoryAsync(CancellationToken ct)
    {
        var category = new Category
        {
            TenantId = DemoTenantId,
            Name = "Genel Urunler",
            Code = "DEMO-GNL",
            Description = "Demo urun kategorisi",
            IsActive = true,
            SortOrder = 1,
            CreatedBy = "DemoDataSeeder"
        };
        SetEntityId(category, DemoCategoryId);

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedDemoCustomerAsync(CancellationToken ct)
    {
        var customer = new Customer
        {
            TenantId = DemoTenantId,
            Name = "Demo Musteri A.S.",
            Code = "DEMO-001",
            CustomerType = "CORPORATE",
            ContactPerson = "Ahmet Yilmaz",
            Email = "demo@musteri.com.tr",
            Phone = "+90 212 555 0001",
            City = "Istanbul",
            Country = "TR",
            IsActive = true,
            CreatedBy = "DemoDataSeeder"
        };
        SetEntityId(customer, DemoCustomerId);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<List<Guid>> SeedDemoProductsAsync(CancellationToken ct)
    {
        var products = new (string Name, string SKU, string Barcode, decimal PurchasePrice, decimal SalePrice, int Stock, string Brand)[]
        {
            ("Pamuklu Basic T-Shirt Beyaz",        "TSH-001", "8690001000001", 45.00m,  149.90m,  120, "MesTech Giyim"),
            ("Organik Zeytinyagi 1L",               "ZYT-002", "8690001000002", 85.00m,  189.90m,   50, "Ege Naturel"),
            ("Bluetooth Kulaklik TWS Pro",          "ELK-003", "8690001000003", 120.00m, 349.90m,   75, "TechSound"),
            ("Seramik Kahve Fincan Seti 6'li",      "MUT-004", "8690001000004", 65.00m,  199.90m,   30, "Porselen Art"),
            ("Dogal Lavanta Sabunu 100g",           "KZM-005", "8690001000005", 12.00m,   39.90m,  200, "Anatolian Herbs"),
            ("Erkek Deri Cuzdan Kahverengi",        "AKS-006", "8690001000006", 95.00m,  259.90m,   45, "DeriMaster"),
            ("Akilli Saat Fitness Tracker",         "ELK-007", "8690001000007", 180.00m, 499.90m,   25, "SmartWear"),
            ("Bambu Mutfak Kesme Tahtasi Set",      "MUT-008", "8690001000008", 35.00m,   99.90m,   60, "EcoKitchen"),
            ("Vitamin C Serum 30ml",                "KZM-009", "8690001000009", 28.00m,   89.90m,  150, "DermaCare"),
            ("Cocuk Egitim Tableti 7 inc",          "ELK-010", "8690001000010", 350.00m, 899.90m,   15, "KidsTech"),
        };

        var productIds = new List<Guid>();

        foreach (var (name, sku, barcode, purchasePrice, salePrice, stock, brand) in products)
        {
            var product = new Product
            {
                TenantId = DemoTenantId,
                Name = name,
                SKU = sku,
                Barcode = barcode,
                PurchasePrice = purchasePrice,
                SalePrice = salePrice,
                ListPrice = salePrice * 1.2m,
                Stock = stock,
                MinimumStock = 5,
                MaximumStock = 500,
                ReorderLevel = 10,
                ReorderQuantity = 50,
                CategoryId = DemoCategoryId,
                Brand = brand,
                TaxRate = 0.20m,
                CurrencyCode = "TRY",
                IsActive = true,
                LastStockUpdate = DateTime.UtcNow,
                CreatedBy = "DemoDataSeeder"
            };

            _context.Products.Add(product);
            productIds.Add(product.Id);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} demo urun olusturuldu", products.Length);

        return productIds;
    }

    private async Task SeedDemoOrdersAsync(List<Guid> productIds, CancellationToken ct)
    {
        var orderData = new (string OrderNumber, OrderStatus Status, int DaysAgo)[]
        {
            ("DEMO-2026-001", OrderStatus.Delivered,  10),
            ("DEMO-2026-002", OrderStatus.Delivered,   7),
            ("DEMO-2026-003", OrderStatus.Shipped,     3),
            ("DEMO-2026-004", OrderStatus.Confirmed,   1),
            ("DEMO-2026-005", OrderStatus.Pending,     0),
        };

        // Pre-fetch product info for order items
        var products = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        var random = new Random(42); // Deterministic seed for reproducibility

        foreach (var (orderNumber, status, daysAgo) in orderData)
        {
            var orderDate = DateTime.UtcNow.AddDays(-daysAgo);

            var order = new Order
            {
                TenantId = DemoTenantId,
                OrderNumber = orderNumber,
                CustomerId = DemoCustomerId,
                Status = status,
                OrderDate = orderDate,
                SourcePlatform = PlatformType.Trendyol,
                CustomerName = "Demo Musteri A.S.",
                CustomerEmail = "demo@musteri.com.tr",
                PaymentStatus = status >= OrderStatus.Confirmed ? "Paid" : "Pending",
                CreatedBy = "DemoDataSeeder"
            };

            // Each order gets 1-3 random items
            var itemCount = random.Next(1, 4);
            var selectedProducts = products
                .OrderBy(_ => random.Next())
                .Take(itemCount)
                .ToList();

            decimal subTotal = 0;
            decimal taxAmount = 0;

            foreach (var product in selectedProducts)
            {
                var quantity = random.Next(1, 4);
                var itemTotal = product.SalePrice * quantity;
                var itemTax = itemTotal * product.TaxRate;

                var orderItem = new OrderItem
                {
                    TenantId = DemoTenantId,
                    OrderId = order.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSKU = product.SKU,
                    Quantity = quantity,
                    UnitPrice = product.SalePrice,
                    TotalPrice = itemTotal,
                    TaxRate = product.TaxRate,
                    TaxAmount = itemTax,
                    CreatedBy = "DemoDataSeeder"
                };

                _context.OrderItems.Add(orderItem);
                subTotal += itemTotal;
                taxAmount += itemTax;
            }

            order.SubTotal = subTotal;
            order.TaxAmount = taxAmount;
            order.TotalAmount = subTotal + taxAmount;
            order.TaxRate = 0.20m;

            _context.Orders.Add(order);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("5 demo siparis olusturuldu");
    }
}
