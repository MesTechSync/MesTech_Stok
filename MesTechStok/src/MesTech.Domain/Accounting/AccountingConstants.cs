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

    /// <summary>102 — Bankalar (Bank Accounts — hakedis tahsilat)</summary>
    public static readonly Guid Account102Banks = new("00000102-0000-0000-0000-000000000000");

    /// <summary>120 — Alıcılar (Trade Receivables)</summary>
    public static readonly Guid Account120Receivables = new("00000120-0000-0000-0000-000000000000");

    /// <summary>153 — Ticari Mallar (Merchandise Inventory — stok maliyeti)</summary>
    public static readonly Guid Account153Inventory = new("00000153-0000-0000-0000-000000000000");

    /// <summary>191 — İndirilecek KDV (VAT Receivable — alış/komisyon KDV'si)</summary>
    public static readonly Guid Account191VatReceivable = new("00000191-0000-0000-0000-000000000000");

    /// <summary>193 — Peşin Ödenen Vergiler (Prepaid Taxes — stopaj mahsubu)</summary>
    public static readonly Guid Account193PrepaidTax = new("00000193-0000-0000-0000-000000000000");

    // ── Kısa Vadeli Borçlar (3xx) ──

    /// <summary>320 — Satıcılar (Trade Payables)</summary>
    public static readonly Guid Account320Payables = new("00000320-0000-0000-0000-000000000000");

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
}
