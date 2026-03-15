namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// Aylik vergi taslagi hazirlandiginda MESA Bot'a bildirim olarak publish edilir.
/// TaxPrepWorker her ayin 1'inde onceki ay icin bu event'i uretir.
/// Exchange: mestech.mesa.finance.tax-prep.ready.v1
/// MESA Bot bu event'i alarak WhatsApp/Telegram uzerinden mali musavire bildirim gonderir.
/// </summary>
public record FinanceTaxPrepReadyEvent(
    int Year,
    int Month,
    decimal TotalSales,
    decimal TotalPurchases,
    decimal CalculatedVAT,
    decimal DeductibleVAT,
    decimal PayableVAT,
    decimal TotalWithholding,
    decimal TotalStopaj,
    string Disclaimer,
    Guid TenantId,
    DateTime OccurredAt);
