namespace MesTech.Application.DTOs;

/// <summary>
/// Bulk Update Result data transfer object.
/// </summary>
public sealed class BulkUpdateResult
{
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<BulkUpdateFailure> Failures { get; init; } = [];
}

public sealed class BulkUpdateFailure
{
    public string Sku { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
