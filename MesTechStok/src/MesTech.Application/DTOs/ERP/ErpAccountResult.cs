namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP cari hesap islem sonucu.
/// </summary>
public record ErpAccountResult(
    bool Success,
    string AccountCode,
    string AccountName,
    decimal Balance,
    string Currency,
    string? ErrorMessage
)
{
    public static ErpAccountResult Ok(string accountCode, string accountName, decimal balance, string currency = "TRY")
        => new(true, accountCode, accountName, balance, currency, null);

    public static ErpAccountResult Failed(string error)
        => new(false, string.Empty, string.Empty, 0m, "TRY", error);
}
