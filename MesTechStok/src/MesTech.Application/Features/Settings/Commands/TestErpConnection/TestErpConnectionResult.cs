namespace MesTech.Application.Features.Settings.Commands.TestErpConnection;

public sealed class TestErpConnectionResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public long ResponseTimeMs { get; init; }

    public static TestErpConnectionResult Success(long responseTimeMs)
        => new() { IsSuccess = true, Message = "Baglanti basarili", ResponseTimeMs = responseTimeMs };

    public static TestErpConnectionResult Failure(string message, long responseTimeMs = 0)
        => new() { IsSuccess = false, Message = message, ResponseTimeMs = responseTimeMs };
}
