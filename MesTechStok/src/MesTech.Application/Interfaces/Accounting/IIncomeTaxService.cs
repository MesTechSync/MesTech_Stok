namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Gelir vergisi hesaplama servisi arayuzu.
/// Turkiye 2026 gelir vergisi dilimleri.
/// </summary>
public interface IIncomeTaxService
{
    /// <summary>
    /// Yillik gelir vergisi hesaplar.
    /// </summary>
    /// <param name="annualIncome">Yillik gelir (TL).</param>
    /// <param name="year">Vergi yili.</param>
    /// <returns>Gelir vergisi hesaplama sonucu.</returns>
    IncomeTaxResultDto CalculateIncomeTax(decimal annualIncome, int year);
}

/// <summary>
/// Gelir vergisi hesaplama sonucu.
/// </summary>
public sealed class IncomeTaxResultDto
{
    public decimal TaxableIncome { get; set; }
    public decimal TotalTax { get; set; }
    public decimal EffectiveRate { get; set; }
    public int Year { get; set; }
    public List<TaxBracketDetailDto> BracketDetails { get; set; } = new();
}

/// <summary>
/// Vergi dilimi detayi.
/// </summary>
public sealed class TaxBracketDetailDto
{
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxableAmountInBracket { get; set; }
    public decimal TaxInBracket { get; set; }
}
