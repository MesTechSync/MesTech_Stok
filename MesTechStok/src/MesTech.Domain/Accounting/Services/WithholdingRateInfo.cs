namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Tevkifat orani bilgisi.
/// </summary>
/// <param name="Code">Oran kodu (ornegin "5/10").</param>
/// <param name="Description">Hizmet/teslim turu aciklamasi.</param>
/// <param name="Rate">Ondalik oran degeri (ornegin 0.50).</param>
public record WithholdingRateInfo(string Code, string Description, decimal Rate);
