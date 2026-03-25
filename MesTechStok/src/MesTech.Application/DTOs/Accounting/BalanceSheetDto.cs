namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Bilanco (Balance Sheet) rapor satiri.
/// Bir hesabin bakiye bilgisini icerir.
/// </summary>
public sealed class BalanceSheetLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

/// <summary>
/// Bilanco bolumu (Assets, Liabilities, Equity).
/// Bolum satirlarini ve bolum toplamini icerir.
/// </summary>
public sealed class BalanceSheetSectionDto
{
    public string SectionName { get; set; } = string.Empty;
    public IReadOnlyList<BalanceSheetLineDto> Lines { get; set; } = Array.Empty<BalanceSheetLineDto>();
    public decimal Total { get; set; }
}

/// <summary>
/// Bilanco (Balance Sheet) raporu.
/// Varlik, Borc ve Ozkaynak bolumlerini icerir.
/// Muhasebe temel denklemi: Assets == Liabilities + Equity.
/// Turkish THP mapping: 1xx=Varliklar, 2xx-3xx=Borclar, 5xx=Ozkaynaklar.
/// </summary>
public sealed class BalanceSheetDto
{
    public DateTime AsOfDate { get; set; }

    /// <summary>Varliklar bolumu (1xx hesaplar).</summary>
    public BalanceSheetSectionDto Assets { get; set; } = new();

    /// <summary>Borclar bolumu (2xx-3xx hesaplar).</summary>
    public BalanceSheetSectionDto Liabilities { get; set; } = new();

    /// <summary>Ozkaynaklar bolumu (5xx hesaplar + donem net kari).</summary>
    public BalanceSheetSectionDto Equity { get; set; } = new();

    /// <summary>
    /// Aktif = Pasif (Borc + Ozkaynak) kontrolu.
    /// Muhasebe temel denklemi: Assets == Liabilities + Equity (her zaman true olmali).
    /// </summary>
    public bool IsBalanced { get; set; }
}
