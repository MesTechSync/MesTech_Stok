using System.Text.Json;
using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün varyantı — renk/beden kombinasyonları + esnek JSON attribute desteği.
/// </summary>
public sealed class ProductVariant : BaseEntity, ITenantEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = false };

    // ── ITenantEntity ──
    public Guid TenantId { get; set; }

    // ── Core ──
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal? Price { get; set; }   // null = use parent Product price
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    // ── Backward-compat fields (kept from original entity) ──
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? VariantSKU { get; set; }
    public string? VariantBarcode { get; set; }
    public decimal? PriceOverride { get; set; }

    // ── D12-02: Dropshipping/entegratör genişletme ──
    public decimal? WeightGrams { get; private set; }
    public decimal? WidthCm { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? DepthCm { get; private set; }
    public int SortOrder { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public string? ImageUrlsJson { get; private set; }

    // ── Flexible attributes stored as JSON text ──
    // Backing field is a Dictionary; AttributesJson is the EF-mapped column.
    private Dictionary<string, string> _attributes = new(StringComparer.Ordinal);

    /// <summary>
    /// Serialised JSON stored in the DB column "Attributes".
    /// EF Core reads/writes this property. Do NOT set directly — use SetAttribute / RemoveAttribute.
    /// </summary>
    public string AttributesJson
    {
        get => JsonSerializer.Serialize(_attributes);
        set
        {
            _attributes = string.IsNullOrWhiteSpace(value)
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(
                    JsonSerializer.Deserialize<Dictionary<string, string>>(value, JsonOptions)
                    ?? new Dictionary<string, string>(StringComparer.Ordinal),
                    StringComparer.Ordinal);
        }
    }

    /// <summary>Flexible attributes (read-only projection over the backing dict).</summary>
    public IReadOnlyDictionary<string, string> Attributes => _attributes.AsReadOnly();

    // ── Navigation ──
    public Product Product { get; set; } = null!;
    public ICollection<ProductPlatformMapping> PlatformMappings { get; set; } = new List<ProductPlatformMapping>();

    // ── EF private constructor ──
    private ProductVariant() { }

    // ── Factory ──
    public static ProductVariant Create(Guid productId, string sku, int stock = 0, decimal? price = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));

        return new ProductVariant
        {
            ProductId = productId,
            SKU = sku,
            Stock = stock,
            Price = price
        };
    }

    // ── Attribute helpers ──

    public void SetAttribute(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributes[key] = value ?? string.Empty;
    }

    public void RemoveAttribute(string key)
    {
        _attributes.Remove(key);
    }

    public string? GetAttribute(string key) => _attributes.GetValueOrDefault(key);

    // ── D12-02: Dimension + image setters (DDD — no public setter) ──

    public void SetDimensions(decimal? weight, decimal? width, decimal? height, decimal? depth)
    {
        WeightGrams = weight;
        WidthCm = width;
        HeightCm = height;
        DepthCm = depth;
    }

    public void SetCompareAtPrice(decimal? compareAtPrice)
    {
        if (compareAtPrice.HasValue && compareAtPrice.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(compareAtPrice), "Karsilastirma fiyati negatif olamaz.");
        CompareAtPrice = compareAtPrice;
    }

    public void SetSortOrder(int order) => SortOrder = order;

    public IReadOnlyList<string> GetImageUrls()
    {
        if (string.IsNullOrWhiteSpace(ImageUrlsJson)) return [];
        return JsonSerializer.Deserialize<List<string>>(ImageUrlsJson) ?? [];
    }

    public void AddImageUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        var urls = GetImageUrls().ToList();
        if (!urls.Contains(url, StringComparer.Ordinal))
        {
            urls.Add(url);
            ImageUrlsJson = JsonSerializer.Serialize(urls);
        }
    }
}
