using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Fatura sablonu — logo, imza, iletisim bilgileri.
/// Her Store'un birden fazla sablonu olabilir, biri IsDefault.
/// </summary>
public class InvoiceTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }
    public string TemplateName { get; set; } = "Varsayilan";

    /// <summary>Sirket logosu — max 500KB. Sorgularda explicit Select ile exclude et.</summary>
    public byte[]? LogoImage { get; set; }

    /// <summary>Islak imza gorseli — max 500KB.</summary>
    public byte[]? SignatureImage { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? TicaretSicilNo { get; set; }
    public bool ShowKargoBarkodu { get; set; }
    public bool ShowFaturaTutariYaziyla { get; set; }
    public bool IsDefault { get; set; }

    // Navigation
    public Store Store { get; set; } = null!;
}
