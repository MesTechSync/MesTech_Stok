using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Crm;

public class CrmContact : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? CustomerId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Company { get; private set; }
    public ContactType Type { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? TaxOffice { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? AssignedToUserId { get; private set; }

    private readonly List<Deal> _deals = [];
    public IReadOnlyCollection<Deal> Deals => _deals.AsReadOnly();

    private CrmContact() { }

    public static CrmContact Create(
        Guid tenantId, string fullName, ContactType type,
        string? email = null, string? phone = null,
        string? company = null, Guid? customerId = null,
        Guid? assignedToUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        return new CrmContact
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            FullName = fullName,
            Email = email,
            Phone = phone,
            Company = company,
            Type = type,
            IsActive = true,
            AssignedToUserId = assignedToUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static CrmContact CreateFromLead(Lead lead, Guid contactId)
    {
        return new CrmContact
        {
            Id = contactId,
            TenantId = lead.TenantId,
            FullName = lead.FullName,
            Email = lead.Email,
            Phone = lead.Phone,
            Company = lead.Company,
            Type = lead.Company is not null ? ContactType.Company : ContactType.Individual,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void LinkToCustomer(Guid customerId)
    {
        CustomerId = customerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
