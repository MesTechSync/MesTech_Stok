namespace MesTech.Application.DTOs;

public class PlatformMappingDto
{
    public string PlatformCode { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public string? ExternalProductId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public bool IsEnabled { get; set; }
}
