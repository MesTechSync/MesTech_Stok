namespace MesTech.Domain.Interfaces;

/// <summary>Kart bilgisi (tokenizasyon icin). Gercek kart numarasi ASLA saklanmaz.</summary>
public record CardInfo(
    string CardHolderName,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string? Cvv = null);
