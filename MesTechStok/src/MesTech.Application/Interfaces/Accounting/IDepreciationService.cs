namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Amortisman hesaplama servisi arayuzu.
/// Dogrusal (linear) ve azalan bakiyeler (declining) yontemlerini destekler.
/// </summary>
public interface IDepreciationService
{
    /// <summary>
    /// Amortisman tablosu hesaplar.
    /// </summary>
    /// <param name="cost">Satin alma maliyeti.</param>
    /// <param name="usefulLifeYears">Faydali omur (yil).</param>
    /// <param name="method">Yontem: "linear" veya "declining".</param>
    /// <returns>Yillik amortisman tablosu.</returns>
    List<DepreciationScheduleDto> CalculateDepreciation(
        decimal cost,
        int usefulLifeYears,
        string method);
}

/// <summary>
/// Yillik amortisman satiri.
/// </summary>
public class DepreciationScheduleDto
{
    public int Year { get; set; }
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
}
