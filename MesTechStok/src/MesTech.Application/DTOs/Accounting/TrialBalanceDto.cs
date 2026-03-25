namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Mizan (Trial Balance) rapor satiri.
/// Her hesap icin acilis, donem ve kapanis borc/alacak bilgisi icerir.
/// </summary>
public sealed class TrialBalanceLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;

    /// <summary>Donem oncesi toplam borc bakiyesi.</summary>
    public decimal OpeningDebit { get; set; }

    /// <summary>Donem oncesi toplam alacak bakiyesi.</summary>
    public decimal OpeningCredit { get; set; }

    /// <summary>Donem icindeki toplam borc hareketi.</summary>
    public decimal PeriodDebit { get; set; }

    /// <summary>Donem icindeki toplam alacak hareketi.</summary>
    public decimal PeriodCredit { get; set; }

    /// <summary>Kapanis borc bakiyesi (Opening + Period).</summary>
    public decimal ClosingDebit { get; set; }

    /// <summary>Kapanis alacak bakiyesi (Opening + Period).</summary>
    public decimal ClosingCredit { get; set; }
}

/// <summary>
/// Mizan raporu — tum hesaplarin acilis/donem/kapanis borc-alacak ozetini icerir.
/// </summary>
public sealed class TrialBalanceDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public IReadOnlyList<TrialBalanceLineDto> Lines { get; set; } = Array.Empty<TrialBalanceLineDto>();
    public decimal GrandTotalOpeningDebit { get; set; }
    public decimal GrandTotalOpeningCredit { get; set; }
    public decimal GrandTotalPeriodDebit { get; set; }
    public decimal GrandTotalPeriodCredit { get; set; }
    public decimal GrandTotalClosingDebit { get; set; }
    public decimal GrandTotalClosingCredit { get; set; }
}
