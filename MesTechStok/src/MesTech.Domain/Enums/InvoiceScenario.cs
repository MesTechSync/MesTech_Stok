namespace MesTech.Domain.Enums;

/// <summary>
/// Fatura senaryosu — GIB profil tipi.
/// EInvoiceScenario (UBL-TR specific) ile karistirma.
/// Bu enum genel fatura akisi icin kullanilir.
/// </summary>
public enum InvoiceScenario
{
    None = 0,           // Belirtilmemis
    Basic = 1,          // Temel fatura
    Commercial = 2,     // Ticari fatura (B2B, kabul/ret dongusu)
    Export = 3          // Ihracat faturasi
}
