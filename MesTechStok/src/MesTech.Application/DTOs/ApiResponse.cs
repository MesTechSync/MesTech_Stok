using System.Diagnostics.CodeAnalysis;

namespace MesTech.Application.DTOs;

/// <summary>
/// Standard API response wrapper — tüm endpoint'ler için tutarlı response shape.
/// SDK/client code generation için kritik: Swagger artık generic "object" yerine typed DTO döner.
///
/// Başarılı: { success: true, data: T, timestamp: "..." }
/// Başarısız: { success: false, error: "...", errorCode: "...", timestamp: "..." }
/// Oluşturma: { success: true, data: { id: "..." }, timestamp: "..." }
/// </summary>
public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

#pragma warning disable CA1000 // Deliberate factory pattern on generic type — Ok()/Fail() API ergonomics
    public static ApiResponse<T> Ok(T data) => new()
    {
        Success = true,
        Data = data,
        Timestamp = DateTime.UtcNow
    };

    public static ApiResponse<T> Fail(string error, string? errorCode = null) => new()
    {
        Success = false,
        Error = error,
        ErrorCode = errorCode,
        Timestamp = DateTime.UtcNow
    };
#pragma warning restore CA1000
}

/// <summary>Standart creation response — { id: Guid }.</summary>
public sealed record CreatedResponse(Guid Id);

/// <summary>Standart list response — { items: T[], totalCount: int }.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>Standart status response — { status: string, message: string }.</summary>
public sealed record StatusResponse(string Status, string? Message = null);

/// <summary>Entity action response — { id: Guid, status: string, message: string }.</summary>
public sealed record EntityActionResponse(Guid Id, string Status, string? Message = null);

/// <summary>Sync operation response — { id: Guid, syncedCount: int }.</summary>
public sealed record SyncResponse(Guid Id, int SyncedCount);

/// <summary>Calendar generation response — { year: int, eventsCreated: int }.</summary>
public sealed record CalendarGenerationResponse(int Year, int EventsCreated);

/// <summary>Row version update response — { newRowVersion: uint }.</summary>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Binary data")]
public sealed record RowVersionResponse(byte[]? NewRowVersion);

/// <summary>Deleted count response — { deletedCount: int }.</summary>
public sealed record DeletedCountResponse(int DeletedCount);

/// <summary>Supported platforms response — { platforms: list, count: int }.</summary>
public sealed record SupportedPlatformsResponse(IReadOnlyList<string> Platforms, int Count);

/// <summary>Chart.js compatible dataset — { labels: string[], datasets: ChartDataset[] }.</summary>
public sealed record ChartResponse(IReadOnlyList<string> Labels, IReadOnlyList<ChartDataset> Datasets);

/// <summary>Single Chart.js dataset — { label: string, data: int[], color: string }.</summary>
public sealed record ChartDataset(string Label, IReadOnlyList<int> Data, string Color);
