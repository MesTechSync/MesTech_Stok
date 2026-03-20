namespace MesTech.Domain.Enums;

public enum InvoiceType
{
    None = 0,
    EFatura = 1,
    EArsiv = 2,
    EIrsaliye = 3,
    EMusFatura = 4,
    ESerbest = 5,
    ESMM = 6,               // e-Serbest Meslek Makbuzu
    EIhracat = 7,           // e-İhracat Faturası
    EWaybill = 10,          // e-İrsaliye
    ESelfEmployment = 11,   // e-SMM
    EExport = 12            // e-İhracat (301-11/1-a istisna)
}
