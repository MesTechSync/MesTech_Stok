namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Komisyon hesaplama sonucu — oran, tutar ve kaynak bilgisi.
/// </summary>
public record CommissionCalculationResult(
    decimal Rate,
    decimal Amount,
    string Source,
    bool IsCached
);
