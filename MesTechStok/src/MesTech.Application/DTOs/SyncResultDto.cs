namespace MesTech.Application.DTOs;

public class SyncResultDto
{
    public bool IsSuccess { get; set; }
    public string PlatformCode { get; set; } = string.Empty;
    public int ItemsProcessed { get; set; }
    public int ItemsFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    public List<string> Warnings { get; set; } = new();
}
