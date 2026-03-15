namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Dinamik oran sonucu — Domain katmaninda tanimlanan ince abstraksiyon.
/// Application katmanindaki ICommissionRateProvider bu sonucu dondurur.
/// </summary>
public record DynamicRateResult(
    decimal Rate,
    string Source,
    DateTime CachedUntil
);
