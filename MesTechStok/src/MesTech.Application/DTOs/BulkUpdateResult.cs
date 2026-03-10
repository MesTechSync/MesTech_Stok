namespace MesTech.Application.DTOs;

public class BulkUpdateResult
{
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<BulkUpdateFailure> Failures { get; init; } = [];
}

public class BulkUpdateFailure
{
    public string Sku { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
