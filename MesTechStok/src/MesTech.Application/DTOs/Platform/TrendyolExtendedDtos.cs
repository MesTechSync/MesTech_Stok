namespace MesTech.Application.DTOs.Platform;

// ──────────────────────────────────────────────
//  Trendyol Extended API DTOs — Dalga 10
// ──────────────────────────────────────────────

/// <summary>
/// Trendyol musteri sorusu.
/// </summary>
public record TrendyolCustomerQuestion(
    long Id,
    string Text,
    long ProductId,
    string Status,
    DateTime CreatedAt);

/// <summary>
/// Trendyol iade/claim bilgisi (basitlestirilmis).
/// </summary>
public record TrendyolClaimDto(
    long Id,
    long OrderId,
    string Reason,
    string Status,
    decimal Amount);

/// <summary>
/// Trendyol hesap ekstre satiri.
/// </summary>
public record TrendyolSettlementItemDto(
    long Id,
    decimal Amount,
    string Currency,
    string Status,
    DateTime Date);

/// <summary>
/// Trendyol tazminat bilgisi.
/// </summary>
public record TrendyolCompensationDto(
    long Id,
    long ClaimId,
    decimal Amount,
    string Status);
