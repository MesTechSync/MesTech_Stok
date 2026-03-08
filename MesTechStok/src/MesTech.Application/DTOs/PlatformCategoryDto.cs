using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs;

/// <summary>
/// Platform-agnostic kategori DTO.
/// Her platformun kategorileri bu DTO ile dondurulur.
/// </summary>
public class PlatformCategoryDto
{
    public string CategoryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentCategoryId { get; set; }
    public bool IsLeaf { get; set; }
    public PlatformType Platform { get; set; }
    public List<PlatformCategoryDto> Children { get; set; } = new();
}
