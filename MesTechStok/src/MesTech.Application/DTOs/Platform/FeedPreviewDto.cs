namespace MesTech.Application.DTOs.Platform;

public class FeedPreviewDto
{
    public int TotalProductCount { get; set; }
    public List<FeedProductPreviewDto> Products { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
