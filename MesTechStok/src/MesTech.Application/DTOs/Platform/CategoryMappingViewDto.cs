namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Category Mapping View data transfer object.
/// </summary>
public class CategoryMappingViewDto
{
    public Guid MappingId { get; set; }
    public Guid InternalCategoryId { get; set; }
    public string InternalCategoryName { get; set; } = string.Empty;
    public string? PlatformCategoryId { get; set; }
    public string? PlatformCategoryName { get; set; }
    public bool IsMapped { get; set; }
}
