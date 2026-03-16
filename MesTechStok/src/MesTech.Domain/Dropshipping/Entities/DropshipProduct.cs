using MesTech.Domain.Common;
using MesTech.Domain.Dropshipping.Enums;

namespace MesTech.Domain.Dropshipping.Entities;

/// <summary>
/// Dropshipping tedarikçisinden gelen harici ürün.
/// MesTech ürünüyle eşleştirildiğinde IsLinked=true olur.
/// </summary>
public class DropshipProduct : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid DropshipSupplierId { get; private set; }
    public string ExternalProductId { get; private set; } = string.Empty;
    public string? ExternalUrl { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal OriginalPrice { get; private set; }
    public decimal SellingPrice { get; private set; }
    public int StockQuantity { get; private set; }

    /// <summary>
    /// MesTech içindeki ürün ID'si. Null ise henüz eşleştirilmemiş.
    /// </summary>
    public Guid? ProductId { get; private set; }

    /// <summary>
    /// MesTech ürünüyle eşleştirilip eşleştirilmediğini belirtir.
    /// </summary>
    public bool IsLinked => ProductId.HasValue;

    public DateTime? LastSyncAt { get; private set; }

    // EF Core parametresiz ctor
    private DropshipProduct() { }

    /// <summary>
    /// Factory method — harici ürün oluşturur.
    /// </summary>
    public static DropshipProduct Create(
        Guid tenantId,
        Guid supplierId,
        string externalProductId,
        string title,
        decimal originalPrice,
        int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalProductId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (originalPrice <= 0)
            throw new ArgumentException("Original price must be greater than zero.", nameof(originalPrice));
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

        return new DropshipProduct
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DropshipSupplierId = supplierId,
            ExternalProductId = externalProductId.Trim(),
            Title = title.Trim(),
            OriginalPrice = originalPrice,
            SellingPrice = originalPrice, // Default: no markup yet
            StockQuantity = stockQuantity,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// MesTech ürünüyle eşleştirir.
    /// </summary>
    public void LinkToProduct(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));

        ProductId = productId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Ürün eşleştirmesini kaldırır.
    /// </summary>
    public void Unlink()
    {
        ProductId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Tedarikçi markup ayarına göre satış fiyatını hesaplar.
    /// </summary>
    public void ApplyMarkup(DropshipMarkupType markupType, decimal markupValue)
    {
        SellingPrice = markupType switch
        {
            DropshipMarkupType.Percentage => OriginalPrice * (1 + markupValue / 100m),
            DropshipMarkupType.FixedAmount => OriginalPrice + markupValue,
            _ => OriginalPrice
        };
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Stok miktarını günceller (feed sync sonucu).
    /// </summary>
    public void UpdateStock(int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(newQuantity));

        StockQuantity = newQuantity;
        LastSyncAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Orijinal fiyatı günceller ve satış fiyatını da sıfırlar (markup tekrar uygulanmalı).
    /// </summary>
    public void UpdatePrice(decimal newOriginalPrice)
    {
        if (newOriginalPrice < 0)
            throw new ArgumentException("Original price cannot be negative.", nameof(newOriginalPrice));

        OriginalPrice = newOriginalPrice;
        SellingPrice = newOriginalPrice; // Markup must be re-applied externally
        LastSyncAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"DropshipProduct [{Title}] Ext:{ExternalProductId} Linked:{IsLinked}";
}
