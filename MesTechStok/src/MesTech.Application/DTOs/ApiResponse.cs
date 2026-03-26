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
}

/// <summary>Standart creation response — { id: Guid }.</summary>
public sealed record CreatedResponse(Guid Id);

/// <summary>Standart list response — { items: T[], totalCount: int }.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>Standart status response — { status: string, message: string }.</summary>
public sealed record StatusResponse(string Status, string? Message = null);
