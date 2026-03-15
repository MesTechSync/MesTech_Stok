namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// N11 SOAP settlement report XML deserialization models.
/// Service: N11 SettlementReport SOAP endpoint.
/// N11 uses SOAP for many of its seller APIs.
/// </summary>
internal sealed class N11SettlementItem
{
    /// <summary>siparisNo — order number.</summary>
    public string? SiparisNo { get; set; }

    /// <summary>urunAdi — product name.</summary>
    public string? UrunAdi { get; set; }

    /// <summary>satisTutari — sale amount (gross).</summary>
    public decimal SatisTutari { get; set; }

    /// <summary>komisyonTutari — commission amount.</summary>
    public decimal KomisyonTutari { get; set; }

    /// <summary>komisyonOrani — commission rate percentage.</summary>
    public decimal KomisyonOrani { get; set; }

    /// <summary>kargoKesinti — cargo deduction.</summary>
    public decimal KargoKesinti { get; set; }

    /// <summary>netTutar — net payout amount.</summary>
    public decimal NetTutar { get; set; }

    /// <summary>islemTarihi — transaction date (yyyy-MM-dd or dd.MM.yyyy).</summary>
    public string? IslemTarihi { get; set; }

    /// <summary>kategori — product category.</summary>
    public string? Kategori { get; set; }
}
