namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Feed Preview data transfer object.
/// </summary>
public class FeedPreviewDto
{
    public int TotalProductCount { get; set; }
    public List<FeedProductPreviewDto> Products { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
