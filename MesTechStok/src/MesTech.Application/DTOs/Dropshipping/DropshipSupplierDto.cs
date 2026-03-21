namespace MesTech.Application.DTOs.Dropshipping;

/// <summary>
/// Dropship Supplier data transfer object.
/// </summary>
public class DropshipSupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? ApiEndpoint { get; set; }
    public string MarkupType { get; set; } = string.Empty;
    public decimal MarkupValue { get; set; }
    public bool AutoSync { get; set; }
    public int SyncIntervalMinutes { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsActive { get; set; }
}
