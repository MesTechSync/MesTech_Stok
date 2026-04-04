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

/// <summary>
/// Trendyol urun degerlendirmesi (review).
/// </summary>
public record TrendyolProductReviewDto(
    long Id,
    long ProductId,
    string Comment,
    int Rate,
    string UserFullName,
    DateTime CreatedAt,
    bool IsReplied);

/// <summary>
/// Trendyol reklam kampanyasi.
/// </summary>
public record TrendyolAdCampaignDto(
    long CampaignId,
    string Name,
    string Status,
    decimal DailyBudget,
    decimal TotalSpent,
    DateTime StartDate,
    DateTime? EndDate);

/// <summary>
/// Trendyol reklam performans metrigi.
/// </summary>
public record TrendyolAdPerformanceDto(
    long CampaignId,
    long Impressions,
    long Clicks,
    decimal Ctr,
    decimal Spend,
    decimal Revenue,
    decimal Acos,
    DateTime Date);
