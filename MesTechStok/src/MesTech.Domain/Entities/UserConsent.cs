using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// KVKK/GDPR consent tracking entity.
/// Her kullanicinin her consent tipi icin kabul/geri cekme kaydi tutulur.
/// 6698 sayili KVKK Madde 5 + GDPR Article 7 geregi.
/// </summary>
public sealed class UserConsent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; private set; }
    public ConsentType ConsentType { get; private set; }
    public int Version { get; private set; }
    public bool IsAccepted { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }
    public string? Notes { get; private set; }

    private UserConsent() { }

    public static UserConsent Accept(
        Guid tenantId,
        Guid userId,
        ConsentType consentType,
        int version,
        string ipAddress,
        string? userAgent = null,
        string? notes = null)
    {
        return new UserConsent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ConsentType = consentType,
            Version = version,
            IsAccepted = true,
            AcceptedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Withdraw(string ipAddress, string? notes = null)
    {
        IsAccepted = false;
        WithdrawnAt = DateTime.UtcNow;
        IpAddress = ipAddress;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ConsentType
{
    /// <summary>KVKK acik riza — zorunlu kisisel veri isleme</summary>
    KvkkExplicit = 1,

    /// <summary>Pazarlama iletisimi — e-posta/SMS/push</summary>
    Marketing = 2,

    /// <summary>Analitik veri toplama — kullanim istatistikleri</summary>
    Analytics = 3,

    /// <summary>Ucuncu taraf paylasim — platform entegrasyonlari</summary>
    ThirdPartySharing = 4,

    /// <summary>Cerez politikasi — web dashboard</summary>
    Cookies = 5
}
