namespace MesTech.Domain.Accounting;

/// <summary>
/// Well-known GL account IDs for the default Turkish Uniform Chart of Accounts (Tek Düzen Hesap Planı).
/// These are deterministic GUIDs derived from account codes — used by GL journal entry handlers.
/// </summary>
public static class AccountingConstants
{
    /// <summary>120 — Alıcılar (Trade Receivables)</summary>
    public static readonly Guid Account120Receivables = new("00000120-0000-0000-0000-000000000000");

    /// <summary>320 — Satıcılar (Trade Payables)</summary>
    public static readonly Guid Account320Payables = new("00000320-0000-0000-0000-000000000000");

    /// <summary>391 — Hesaplanan KDV (VAT Payable)</summary>
    public static readonly Guid Account391VatPayable = new("00000391-0000-0000-0000-000000000000");

    /// <summary>600 — Yurtiçi Satışlar (Domestic Sales Revenue)</summary>
    public static readonly Guid Account600DomesticSales = new("00000600-0000-0000-0000-000000000000");

    /// <summary>610 — Satıştan İadeler (Sales Returns)</summary>
    public static readonly Guid Account610SalesReturns = new("00000610-0000-0000-0000-000000000000");

    /// <summary>760 — Pazarlama Satış Dağıtım Giderleri (Commission + Cargo costs)</summary>
    public static readonly Guid Account760MarketingExpenses = new("00000760-0000-0000-0000-000000000000");
}
