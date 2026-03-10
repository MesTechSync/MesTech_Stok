using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün Seti Aggregate Root — birden fazla ürünü bir paket olarak satar.
/// Satış yapıldığında her bileşenin stoğu otomatik düşürülür.
/// Dairesel referans yapısal olarak imkansızdır: set yalnızca Product (Guid) içerir.
/// </summary>
public class ProductSet : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    private readonly List<ProductSetItem> _items = new();
    public IReadOnlyCollection<ProductSetItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Sete ürün ekler. Aynı ürün iki kez eklenemez.
    /// </summary>
    public void AddItem(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be at least 1.");
        if (_items.Any(i => i.ProductId == productId))
            throw new InvalidOperationException("Product already in set.");

        var item = ProductSetItem.Create(Id, productId, quantity);
        _items.Add(item);
    }

    /// <summary>
    /// Setten ürün çıkarır. Bulunamazsa exception fırlatır.
    /// </summary>
    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException($"Product {productId} not in set.");
        _items.Remove(item);
    }

    /// <summary>
    /// Stok düşümü için (ProductId, Quantity) çiftlerini döner.
    /// </summary>
    public IReadOnlyList<(Guid ProductId, int Quantity)> GetStockDeductions()
        => _items.Select(i => (i.ProductId, i.Quantity)).ToList().AsReadOnly();
}
