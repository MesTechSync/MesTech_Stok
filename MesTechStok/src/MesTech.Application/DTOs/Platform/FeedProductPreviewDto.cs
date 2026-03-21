namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Feed Product Preview data transfer object.
/// </summary>
public class FeedProductPreviewDto
{
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public decimal SupplierPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public int Stock { get; set; }
    public bool AlreadyExists { get; set; }
}
