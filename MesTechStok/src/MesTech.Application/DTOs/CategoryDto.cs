namespace MesTech.Application.DTOs;

/// <summary>
/// Platform kategori bilgisi.
/// </summary>
public class CategoryDto
{
    public int PlatformCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
}

/// <summary>
/// Platform marka bilgisi.
/// </summary>
public class BrandDto
{
    public int PlatformBrandId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Kategori ozelligi (attribute) bilgisi — urun olusturmada zorunlu alanlar.
/// </summary>
public class CategoryAttributeDto
{
    public int AttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool AllowCustom { get; set; }
    public string? VariantType { get; set; }
    public List<CategoryAttributeValueDto> Values { get; set; } = new();
}

/// <summary>
/// Kategori ozelligi deger secenegi.
/// </summary>
public class CategoryAttributeValueDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Batch islem sonucu — async Trendyol islemleri icin.
/// </summary>
public class BatchRequestResultDto
{
    public string BatchRequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int FailedItemCount { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? LastModification { get; set; }
    public List<BatchItemDto> Items { get; set; } = new();
}

/// <summary>
/// Batch islem icerisindeki tekil kayit sonucu.
/// </summary>
public class BatchItemDto
{
    public string? RequestItem { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> FailureReasons { get; set; } = new();
}

/// <summary>
/// Platform API saglik durumu.
/// </summary>
public class PlatformHealthDto
{
    public string PlatformCode { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public int LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
