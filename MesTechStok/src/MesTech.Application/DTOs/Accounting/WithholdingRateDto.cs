namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// KDV tevkifat orani DTO'su — GiB resmi listesi.
/// </summary>
public sealed class WithholdingRateDto
{
    /// <summary>Oran kodu (ornegin "5/10").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Hizmet/teslim turu aciklamasi.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ondalik oran degeri (ornegin 0.50).</summary>
    public decimal Rate { get; set; }

    /// <summary>Yuzde olarak gosterim (ornegin %50).</summary>
    public string DisplayPercent { get; set; } = string.Empty;
}
