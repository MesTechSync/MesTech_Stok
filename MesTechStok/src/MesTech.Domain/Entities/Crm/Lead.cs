using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Crm;

namespace MesTech.Domain.Entities.Crm;

public class Lead : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Company { get; private set; }
    public LeadSource Source { get; private set; }
    public LeadStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? ContactedAt { get; private set; }
    public DateTime? ConvertedAt { get; private set; }
    public Guid? ConvertedToCrmContactId { get; private set; }

    private Lead() { }

    public static Lead Create(
        Guid tenantId, string fullName, LeadSource source,
        string? email = null, string? phone = null,
        string? company = null, Guid? storeId = null,
        Guid? assignedToUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        return new Lead
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreId = storeId,
            FullName = fullName,
            Email = email,
            Phone = phone,
            Company = company,
            Source = source,
            Status = LeadStatus.New,
            AssignedToUserId = assignedToUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsContacted(string? notes = null)
    {
        if (Status == LeadStatus.Converted || Status == LeadStatus.Lost)
            throw new InvalidOperationException("Cannot contact a converted or lost lead.");

        Status = LeadStatus.Contacted;
        ContactedAt = DateTime.UtcNow;
        if (notes is not null) Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Qualify(string notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notes);
        Status = LeadStatus.Qualified;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsLost(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = LeadStatus.Lost;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Convert()
    {
        if (Status == LeadStatus.Converted)
            throw new InvalidOperationException("Lead is already converted.");
        if (Status == LeadStatus.Lost)
            throw new InvalidOperationException("Cannot convert a lost lead.");

        var contactId = Guid.NewGuid();
        Status = LeadStatus.Converted;
        ConvertedAt = DateTime.UtcNow;
        ConvertedToCrmContactId = contactId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new LeadConvertedEvent(Id, TenantId, contactId, DateTime.UtcNow));
        return contactId;
    }
}
