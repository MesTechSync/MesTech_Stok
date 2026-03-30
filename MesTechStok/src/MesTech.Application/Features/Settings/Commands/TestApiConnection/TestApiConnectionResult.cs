namespace MesTech.Application.Features.Settings.Commands.TestApiConnection;

public sealed class TestApiConnectionResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public long ResponseTimeMs { get; init; }
    public int StatusCode { get; init; }

    public static TestApiConnectionResult Success(long responseTimeMs, int statusCode)
        => new() { IsSuccess = true, Message = "Baglanti basarili", ResponseTimeMs = responseTimeMs, StatusCode = statusCode };

    public static TestApiConnectionResult Failure(string message, long responseTimeMs = 0, int statusCode = 0)
        => new() { IsSuccess = false, Message = message, ResponseTimeMs = responseTimeMs, StatusCode = statusCode };
}
