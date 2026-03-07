namespace MesTech.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 0,
    Queued = 1,
    Sent = 2,
    Accepted = 3,
    Rejected = 4,
    Cancelled = 5,
    Error = 6,
    PlatformSent = 7
}
