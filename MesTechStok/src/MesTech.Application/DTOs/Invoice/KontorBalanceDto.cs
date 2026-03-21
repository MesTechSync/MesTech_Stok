namespace MesTech.Application.DTOs.Invoice;

/// <summary>
/// Kontor Balance data transfer object.
/// </summary>
public record KontorBalanceDto(
    int RemainingKontor,
    int TotalKontor,
    DateTime? ExpiresAt,
    string ProviderName);
