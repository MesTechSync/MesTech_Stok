namespace MesTech.Domain.Enums;

/// <summary>
/// E-Fatura belge durumu (yasam dongusu).
/// </summary>
public enum EInvoiceStatus
{
    Draft = 0,
    Sending = 1,
    Sent = 2,
    Accepted = 3,
    Rejected = 4,
    Cancelled = 5,
    Error = 6
}
