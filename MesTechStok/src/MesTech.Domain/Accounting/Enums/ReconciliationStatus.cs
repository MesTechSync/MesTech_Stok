namespace MesTech.Domain.Accounting.Enums;

/// <summary>
/// Mutabakat eslestirme durumu.
/// </summary>
public enum ReconciliationStatus
{
    AutoMatched = 0,
    NeedsReview = 1,
    Rejected = 2,
    ManualMatch = 3
}
