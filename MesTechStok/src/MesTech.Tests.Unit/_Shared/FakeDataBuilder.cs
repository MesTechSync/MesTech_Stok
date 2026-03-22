using Bogus;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit._Shared;

/// <summary>
/// Bogus ile gercekci test verisi ureten builder.
/// </summary>
public static class FakeDataBuilder
{
    private static readonly Faker _faker = new("tr");

    public static Product CreateProduct(
        string? sku = null,
        string? name = null,
        int stock = 100,
        decimal purchasePrice = 50m,
        decimal salePrice = 100m,
        Guid? categoryId = null,
        Guid? tenantId = null)
    {
        var product = new Product
        {
            Name = name ?? _faker.Commerce.ProductName(),
            SKU = sku ?? _faker.Random.AlphaNumeric(8).ToUpperInvariant(),
            Barcode = _faker.Random.ReplaceNumbers("869#########"),
            Stock = stock,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            CategoryId = categoryId ?? Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            MinimumStock = 5,
            MaximumStock = 1000,
            ReorderLevel = 10,
            TaxRate = 0.20m,
            IsActive = true,
            CurrencyCode = "TRY"
        };
        return product;
    }

    public static StockMovement CreateStockMovement(
        Guid? productId = null,
        int quantity = 10,
        StockMovementType type = StockMovementType.StockIn,
        int previousStock = 0)
    {
        var movement = new StockMovement
        {
            ProductId = productId ?? Guid.NewGuid(),
            Quantity = quantity,
            UnitCost = _faker.Random.Decimal(10, 500),
            Date = DateTime.UtcNow,
            Reason = _faker.Lorem.Sentence()
        };
        movement.SetStockLevels(previousStock, previousStock + quantity);
        movement.SetMovementType(type);
        return movement;
    }

    public static Category CreateCategory(
        string? name = null,
        Guid? parentId = null)
    {
        return new Category
        {
            Name = name ?? _faker.Commerce.Categories(1)[0],
            Code = _faker.Random.AlphaNumeric(6).ToUpperInvariant(),
            ParentCategoryId = parentId,
            IsActive = true,
            SortOrder = _faker.Random.Int(1, 100)
        };
    }

    public static Order CreateOrder(
        Guid? customerId = null,
        OrderStatus status = OrderStatus.Pending)
    {
        var order = new Order
        {
            OrderNumber = $"ORD-{_faker.Random.Int(10000, 99999)}",
            CustomerId = customerId ?? Guid.NewGuid(),
            Status = status,
            OrderDate = DateTime.UtcNow,
            TaxRate = 0.20m
        };
        order.SetFinancials(0, 0, 0);
        return order;
    }

    public static User CreateUser(
        string? username = null,
        string? passwordHash = null,
        bool isActive = true)
    {
        return new User
        {
            Username = username ?? _faker.Internet.UserName(),
            Email = _faker.Internet.Email(),
            PasswordHash = passwordHash ?? "hashed_password",
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            IsActive = isActive
        };
    }

    public static InventoryLot CreateLot(
        Guid? productId = null,
        decimal receivedQty = 100,
        decimal remainingQty = 100,
        DateTime? expiryDate = null,
        LotStatus status = LotStatus.Open)
    {
        return new InventoryLot
        {
            ProductId = productId ?? Guid.NewGuid(),
            LotNumber = $"LOT-{_faker.Random.Int(1000, 9999)}",
            ReceivedQty = receivedQty,
            RemainingQty = remainingQty,
            ExpiryDate = expiryDate,
            Status = status,
            CreatedDate = DateTime.UtcNow
        };
    }

    public static string GenerateValidEAN13()
    {
        var digits = new int[12];
        digits[0] = 8; digits[1] = 6; digits[2] = 9; // Turkey prefix
        for (int i = 3; i < 12; i++)
            digits[i] = _faker.Random.Int(0, 9);

        int sum = 0;
        for (int i = 0; i < 12; i++)
            sum += i % 2 == 0 ? digits[i] : digits[i] * 3;

        int checkDigit = (10 - (sum % 10)) % 10;
        return string.Concat(digits.Select(d => d.ToString())) + checkDigit;
    }
}
