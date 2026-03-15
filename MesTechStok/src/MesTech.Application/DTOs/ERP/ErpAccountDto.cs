namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP tarafindaki hesap bakiye bilgisi.
/// GetAccountBalancesAsync sonucunda doner.
/// Dalga 11: ERP entegrasyonu icin eklendi.
/// </summary>
public record ErpAccountDto(
    string AccountCode,
    string AccountName,
    decimal Balance,
    string Currency
);
