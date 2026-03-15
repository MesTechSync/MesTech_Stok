using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.RunReconciliation;

/// <summary>
/// Otomatik mutabakat eslestirme komutu.
/// Tum eslestirilmemis SettlementBatch ve BankTransaction ciftlerini degerlendirir.
/// </summary>
public record RunReconciliationCommand(Guid TenantId) : IRequest<RunReconciliationResult>;

/// <summary>
/// Mutabakat calistirma sonucu — eslestirme istatistikleri.
/// </summary>
public record RunReconciliationResult
{
    public int AutoMatchedCount { get; init; }
    public int NeedsReviewCount { get; init; }
    public int UnmatchedCount { get; init; }
    public decimal AutoMatchedTotal { get; init; }
    public decimal NeedsReviewTotal { get; init; }
}
