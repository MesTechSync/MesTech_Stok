using Bogus;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit._Shared;

public static class FakeData
{
    public static Product CreateProduct(
        string? sku = null,
        string? barcode = null,
        decimal purchasePrice = 100m,
        decimal salePrice = 150m,
        int stock = 50,
        int minimumStock = 5)
    {
        var faker = new Faker("tr");
        return new Product
        {
            Name = faker.Commerce.ProductName(),
            SKU = sku ?? faker.Random.AlphaNumeric(10).ToUpperInvariant(),
            Barcode = barcode ?? faker.Random.Long(1000000000000, 9999999999999).ToString(),
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            Stock = stock,
            MinimumStock = minimumStock,
            MaximumStock = 1000,
            ReorderLevel = 10,
            CategoryId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            IsActive = true,
        };
    }

    public static InventoryLot CreateLot(
        Guid productId,
        decimal receivedQty = 100,
        decimal remainingQty = 50,
        DateTime? expiryDate = null)
    {
        return new InventoryLot
        {
            ProductId = productId,
            LotNumber = $"LOT-{Guid.NewGuid().ToString()[..8]}",
            ExpiryDate = expiryDate ?? DateTime.UtcNow.AddMonths(6),
            ReceivedQty = receivedQty,
            RemainingQty = remainingQty,
            Status = LotStatus.Open,
        };
    }

    public static Tenant CreateTenant(string? name = null)
    {
        var faker = new Faker("tr");
        return new Tenant
        {
            Name = name ?? faker.Company.CompanyName(),
            TaxNumber = faker.Random.Long(1000000000, 9999999999).ToString(),
            IsActive = true,
        };
    }

    public static Store CreateStore(Guid tenantId, PlatformType platform = PlatformType.Trendyol)
    {
        return new Store
        {
            TenantId = tenantId,
            PlatformType = platform,
            StoreName = $"{platform} Store",
            ExternalStoreId = Guid.NewGuid().ToString()[..8],
            IsActive = true,
        };
    }

    public static Order CreateOrder(
        Guid? customerId = null,
        OrderStatus status = OrderStatus.Pending,
        PlatformType? sourcePlatform = null)
    {
        var faker = new Faker("tr");
        var order = new Order
        {
            OrderNumber = $"ORD-{faker.Random.Int(10000, 99999)}",
            CustomerId = customerId ?? Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Status = status,
            SourcePlatform = sourcePlatform,
        };
        order.SetFinancials(
            faker.Random.Decimal(50, 5000),
            faker.Random.Decimal(10, 500),
            faker.Random.Decimal(60, 5500));
        return order;
    }

    public static CiceksepetiCategory CreateCiceksepetiCategory(
        long categoryId = 0,
        string? name = null,
        long? parentId = null,
        bool isLeaf = false)
    {
        var faker = new Faker("tr");
        return new CiceksepetiCategory
        {
            CiceksepetiCategoryId = categoryId == 0 ? faker.Random.Long(1000, 99999) : categoryId,
            CategoryName = name ?? faker.Commerce.Categories(1).First(),
            ParentCategoryId = parentId,
            IsLeaf = isLeaf,
        };
    }

    public static HepsiburadaListing CreateHepsiburadaListing(
        string? hbSku = null,
        string? merchantSku = null,
        string status = "Passive",
        decimal commission = 0.12m)
    {
        var faker = new Faker("tr");
        return new HepsiburadaListing
        {
            HepsiburadaSKU = hbSku ?? $"HB-{faker.Random.AlphaNumeric(8)}",
            MerchantSKU = merchantSku ?? $"MRC-{faker.Random.AlphaNumeric(6)}",
            ListingStatus = status,
            CommissionRate = commission,
        };
    }
}
