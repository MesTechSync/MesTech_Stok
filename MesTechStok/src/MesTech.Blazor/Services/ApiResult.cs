namespace MesTech.Blazor.Services;

/// <summary>
/// Wraps every API call result.
/// On success — <see cref="IsSuccess"/> is true and <see cref="Data"/> is populated.
/// On fallback — <see cref="IsDemoMode"/> is true, the page falls back to demo data,
/// and <see cref="ErrorMessage"/> carries a user-friendly reason.
/// </summary>
public sealed class ApiResult<T> where T : class
{
    public bool IsSuccess { get; private set; }
    public bool IsDemoMode { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ApiResult<T> Success(T data) =>
        new() { IsSuccess = true, Data = data };

    public static ApiResult<T> Fallback(string message) =>
        new() { IsDemoMode = true, ErrorMessage = message };
}
