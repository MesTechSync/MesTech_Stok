using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün seti bileşen kalemi — ProductSet aggregate'e ait value-like entity.
/// Her kalem, sette kaç adet o ürünün bulunduğunu tanımlar.
/// </summary>
public class ProductSetItem : BaseEntity
{
    public Guid ProductSetId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    private ProductSetItem() { }  // EF constructor

    public static ProductSetItem Create(Guid productSetId, Guid productId, int quantity)
    {
        if (productSetId == Guid.Empty)
            throw new ArgumentException("ProductSetId cannot be empty.", nameof(productSetId));
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be at least 1.");

        return new ProductSetItem
        {
            ProductSetId = productSetId,
            ProductId = productId,
            Quantity = quantity
        };
    }
}
