namespace MesTech.Domain.Accounting;

/// <summary>
/// Well-known GL account IDs for the default Turkish Uniform Chart of Accounts (Tek Düzen Hesap Planı).
/// These are deterministic GUIDs derived from account codes — used by GL journal entry handlers.
/// </summary>
public static class AccountingConstants
{
    // ── Dönen Varlıklar (1xx) ──

    /// <summary>100 — Kasa (Cash)</summary>
    public static readonly Guid Account100Cash = new("00000100-0000-0000-0000-000000000000");

    /// <summary>101 — Alınan Çekler (Received Cheques)</summary>
    public static readonly Guid Account101ReceivedCheques = new("00000101-0000-0000-0000-000000000000");

    /// <summary>102 — Bankalar (Bank Accounts — hakedis tahsilat)</summary>
    public static readonly Guid Account102Banks = new("00000102-0000-0000-0000-000000000000");

    /// <summary>103 — Verilen Çekler ve Ödeme Emirleri</summary>
    public static readonly Guid Account103GivenCheques = new("00000103-0000-0000-0000-000000000000");

    /// <summary>120 — Alıcılar (Trade Receivables)</summary>
    public static readonly Guid Account120Receivables = new("00000120-0000-0000-0000-000000000000");

    /// <summary>121 — Alacak Senetleri (Notes Receivable)</summary>
    public static readonly Guid Account121NotesReceivable = new("00000121-0000-0000-0000-000000000000");

    /// <summary>153 — Ticari Mallar (Merchandise Inventory — stok maliyeti)</summary>
    public static readonly Guid Account153Inventory = new("00000153-0000-0000-0000-000000000000");

    /// <summary>191 — İndirilecek KDV (VAT Receivable — alış/komisyon KDV'si)</summary>
    public static readonly Guid Account191VatReceivable = new("00000191-0000-0000-0000-000000000000");

    /// <summary>193 — Peşin Ödenen Vergiler (Prepaid Taxes — stopaj mahsubu)</summary>
    public static readonly Guid Account193PrepaidTax = new("00000193-0000-0000-0000-000000000000");

    // ── Kısa Vadeli Borçlar (3xx) ──

    /// <summary>320 — Satıcılar (Trade Payables)</summary>
    public static readonly Guid Account320Payables = new("00000320-0000-0000-0000-000000000000");

    /// <summary>321 — Borç Senetleri (Notes Payable)</summary>
    public static readonly Guid Account321NotesPayable = new("00000321-0000-0000-0000-000000000000");

    /// <summary>360 — Ödenecek Vergi ve Fonlar (Taxes Payable)</summary>
    public static readonly Guid Account360TaxesPayable = new("00000360-0000-0000-0000-000000000000");

    /// <summary>380 — Gelecek Aylara Ait Gelirler (Deferred Revenue)</summary>
    public static readonly Guid Account380DeferredRevenue = new("00000380-0000-0000-0000-000000000000");

    /// <summary>391 — Hesaplanan KDV (VAT Payable)</summary>
    public static readonly Guid Account391VatPayable = new("00000391-0000-0000-0000-000000000000");

    // ── Gelirler (6xx) ──

    /// <summary>600 — Yurtiçi Satışlar (Domestic Sales Revenue)</summary>
    public static readonly Guid Account600DomesticSales = new("00000600-0000-0000-0000-000000000000");

    /// <summary>610 — Satıştan İadeler (Sales Returns)</summary>
    public static readonly Guid Account610SalesReturns = new("00000610-0000-0000-0000-000000000000");

    /// <summary>621 — Satılan Ticari Mallar Maliyeti (COGS — FIFO/ortalama)</summary>
    public static readonly Guid Account621Cogs = new("00000621-0000-0000-0000-000000000000");

    // ── Giderler (7xx) ──

    /// <summary>760 — Pazarlama Satış Dağıtım Giderleri (Commission + Cargo costs)</summary>
    public static readonly Guid Account760MarketingExpenses = new("00000760-0000-0000-0000-000000000000");

    // ── Özkaynaklar (5xx) ──

    /// <summary>500 — Sermaye (Capital)</summary>
    public static readonly Guid Account500Capital = new("00000500-0000-0000-0000-000000000000");

    /// <summary>570 — Geçmiş Yıllar Kârları (Retained Earnings)</summary>
    public static readonly Guid Account570RetainedEarnings = new("00000570-0000-0000-0000-000000000000");

    /// <summary>580 — Geçmiş Yıllar Zararları (Accumulated Losses)</summary>
    public static readonly Guid Account580AccumulatedLosses = new("00000580-0000-0000-0000-000000000000");

    /// <summary>690 — Dönem Kârı veya Zararı (Net Income/Loss)</summary>
    public static readonly Guid Account690NetIncome = new("00000690-0000-0000-0000-000000000000");
}
