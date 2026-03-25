namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Fetched Product data transfer object.
/// </summary>
public sealed class FetchedProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? Description { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
