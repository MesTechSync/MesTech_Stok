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
            Id = faker.Random.Int(1, 10000),
            Name = faker.Commerce.ProductName(),
            SKU = sku ?? faker.Random.AlphaNumeric(10).ToUpperInvariant(),
            Barcode = barcode ?? faker.Random.Long(1000000000000, 9999999999999).ToString(),
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            Stock = stock,
            MinimumStock = minimumStock,
            MaximumStock = 1000,
            ReorderLevel = 10,
            CategoryId = 1,
            TenantId = 1,
            IsActive = true,
        };
    }

    public static InventoryLot CreateLot(
        int productId,
        decimal receivedQty = 100,
        decimal remainingQty = 50,
        DateTime? expiryDate = null)
    {
        return new InventoryLot
        {
            Id = new Faker().Random.Int(1, 10000),
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
            Id = faker.Random.Int(1, 100),
            Name = name ?? faker.Company.CompanyName(),
            TaxNumber = faker.Random.Long(1000000000, 9999999999).ToString(),
            IsActive = true,
        };
    }

    public static Store CreateStore(int tenantId, PlatformType platform = PlatformType.Trendyol)
    {
        return new Store
        {
            Id = new Faker().Random.Int(1, 1000),
            TenantId = tenantId,
            PlatformType = platform,
            StoreName = $"{platform} Store",
            ExternalStoreId = Guid.NewGuid().ToString()[..8],
            IsActive = true,
        };
    }
}
