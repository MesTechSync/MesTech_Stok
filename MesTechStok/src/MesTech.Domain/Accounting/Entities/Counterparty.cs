using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Karsi taraf (cari) — platform, tedarikci, musteri, banka veya kargoci.
/// VKN sifreli saklanir.
/// </summary>
public class Counterparty : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? VKN { get; private set; }
    public CounterpartyType CounterpartyType { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? Platform { get; private set; }
    public bool IsActive { get; private set; }

    private Counterparty() { }

    public static Counterparty Create(
        Guid tenantId,
        string name,
        CounterpartyType counterpartyType,
        string? vkn = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? platform = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Counterparty
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            VKN = vkn,
            CounterpartyType = counterpartyType,
            Phone = phone,
            Email = email,
            Address = address,
            Platform = platform,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? vkn,
        string? phone,
        string? email,
        string? address,
        string? platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        VKN = vkn;
        Phone = phone;
        Email = email;
        Address = address;
        Platform = platform;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
