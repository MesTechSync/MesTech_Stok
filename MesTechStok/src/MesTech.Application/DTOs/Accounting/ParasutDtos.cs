namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Paraşüt'e kayıt gönderme sonucu.
/// </summary>
public class ParasutSyncResult
{
    public bool Success { get; init; }
    public string? ExternalId { get; init; }     // Paraşüt record ID
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Paraşüt'ten çekilen cari bakiye özeti.
/// </summary>
public class ParasutBalanceDto
{
    public decimal TotalReceivable { get; init; }    // Toplam alacak
    public decimal TotalPayable { get; init; }       // Toplam borç
    public decimal NetBalance { get; init; }         // Net bakiye
    public DateTime AsOf { get; init; }
}

/// <summary>
/// Paraşüt'ten çekilen tek bir hareket kaydı.
/// </summary>
public class ParasutTransactionDto
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;   // "income" | "expense"
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime Date { get; init; }
}
