using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetPendingReviews;

/// <summary>
/// Inceleme bekleyen mutabakat eslestirmelerini sayfalanmis olarak getirir.
/// NeedsReview durumundakiler, Confidence azalan sirada.
/// </summary>
public record GetPendingReviewsQuery(
    Guid TenantId,
    int PageSize = 20,
    int Page = 1
) : IRequest<PendingReviewsResult>;

/// <summary>
/// Sayfalanmis inceleme sonucu.
/// </summary>
public record PendingReviewsResult
{
    public IReadOnlyList<PendingReviewMatchDto> Items { get; init; } = Array.Empty<PendingReviewMatchDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>
/// Inceleme bekleyen eslestirme DTO — skor, settlement ve banka tx bilgileriyle.
/// </summary>
public record PendingReviewMatchDto
{
    public Guid MatchId { get; init; }
    public decimal Confidence { get; init; }
    public DateTime MatchDate { get; init; }

    // Settlement batch info
    public Guid? SettlementBatchId { get; init; }
    public string? SettlementPlatform { get; init; }
    public DateTime? SettlementPeriodStart { get; init; }
    public DateTime? SettlementPeriodEnd { get; init; }
    public decimal? SettlementTotalNet { get; init; }

    // Bank transaction info
    public Guid? BankTransactionId { get; init; }
    public DateTime? BankTransactionDate { get; init; }
    public decimal? BankTransactionAmount { get; init; }
    public string? BankTransactionDescription { get; init; }
}
