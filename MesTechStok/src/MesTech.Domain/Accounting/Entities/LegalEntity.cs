using MesTech.Domain.Accounting.ValueObjects;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Tüzel kisilik — firma bilgileri.
/// VKN sifreli saklanir.
/// </summary>
public sealed class LegalEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string TaxNumber { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsDefault { get; private set; }

    private LegalEntity() { }

    public static LegalEntity Create(
        Guid tenantId,
        string name,
        string taxNumber,
        string? address = null,
        string? phone = null,
        string? email = null,
        bool isDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(taxNumber);

        return new LegalEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            TaxNumber = taxNumber,
            Address = address,
            Phone = phone,
            Email = email,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? address, string? phone, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Address = address;
        Phone = phone;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }
}
