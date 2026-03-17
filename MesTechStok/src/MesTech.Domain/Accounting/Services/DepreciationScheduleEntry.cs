namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Amortisman takvimi satiri.
/// </summary>
/// <param name="Year">Yil sirasi (1, 2, 3...).</param>
/// <param name="DepreciationAmount">Yillik amortisman tutari.</param>
/// <param name="AccumulatedDepreciation">Birikmis amortisman.</param>
/// <param name="NetBookValue">Kalan net defter degeri.</param>
public record DepreciationScheduleEntry(
    int Year,
    decimal DepreciationAmount,
    decimal AccumulatedDepreciation,
    decimal NetBookValue);
