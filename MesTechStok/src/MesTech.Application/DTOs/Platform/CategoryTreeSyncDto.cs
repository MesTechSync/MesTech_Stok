namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Platform-agnostic kategori agac yapisi DTO'su.
/// Parent-child iliskisi ile hiyerarsik kategori senkronizasyonu.
/// </summary>
public sealed class CategoryTreeSyncDto
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Status { get; set; }
    public List<CategoryTreeSyncDto> Children { get; set; } = new();
}

/// <summary>
/// Platform-agnostic kategori push DTO'su.
/// Yeni kategori olusturma veya mevcut guncelleme icin kullanilir.
/// </summary>
public sealed class CategorySyncDto
{
    public string? Id { get; set; }
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool Status { get; set; } = true;
    public string? ImageUrl { get; set; }
}
