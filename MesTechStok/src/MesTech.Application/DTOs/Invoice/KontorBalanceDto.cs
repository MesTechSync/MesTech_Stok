namespace MesTech.Application.DTOs.Invoice;

public record KontorBalanceDto(
    int RemainingKontor,
    int TotalKontor,
    DateTime? ExpiresAt,
    string ProviderName);
