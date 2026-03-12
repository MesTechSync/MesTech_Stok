using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// MesTech Customer ↔ Bitrix24 Contact mapping.
/// crm.contact.add sonucu — ExternalContactId Bitrix24'teki contact ID'si.
/// </summary>
public class Bitrix24Contact : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string ExternalContactId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? CompanyTitle { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.NotSynced;
    public DateTime? LastSyncDate { get; set; }
    public string? SyncError { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
}
