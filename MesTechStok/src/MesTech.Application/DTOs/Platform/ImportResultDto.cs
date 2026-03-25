namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Import Result data transfer object.
/// </summary>
public sealed class ImportResultDto
{
    public int TotalProcessed { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess => Errors.Count == 0;
}
