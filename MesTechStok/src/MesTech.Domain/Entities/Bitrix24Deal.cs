using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// MesTech Order ↔ Bitrix24 Deal mapping.
/// crm.deal.add sonucu — ExternalDealId Bitrix24'teki deal ID'si.
/// </summary>
public class Bitrix24Deal : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public string ExternalDealId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Opportunity { get; set; }
    public string StageId { get; set; } = "NEW";
    public string? CategoryId { get; set; }
    public string? AssignedById { get; set; }
    public string Currency { get; set; } = "TRY";
    public SyncStatus SyncStatus { get; set; } = SyncStatus.NotSynced;
    public DateTime? LastSyncDate { get; set; }
    public string? SyncError { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public ICollection<Bitrix24DealProductRow> ProductRows { get; set; } = new List<Bitrix24DealProductRow>();
}
