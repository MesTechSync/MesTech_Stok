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
