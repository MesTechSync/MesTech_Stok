namespace MesTech.Application.DTOs;

/// <summary>
/// Bitrix24 Deal Status data transfer object.
/// </summary>
public class Bitrix24DealStatusDto
{
    public Guid Bitrix24DealId { get; set; }
    public string ExternalDealId { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Opportunity { get; set; }
    public string StageId { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = string.Empty;
    public DateTime? LastSyncDate { get; set; }
    public string? SyncError { get; set; }
}
