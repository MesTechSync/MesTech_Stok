namespace MesTech.Application.DTOs;

/// <summary>
/// Birden fazla platformun toplu sync sonucu.
/// Dashboard'da gosterilir, log'a yazilir.
/// </summary>
public class SyncBatchResultDto
{
    public int TotalPlatforms { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public List<SyncResultDto> PlatformResults { get; set; } = new();

    public bool AllSucceeded => FailureCount == 0;
}
