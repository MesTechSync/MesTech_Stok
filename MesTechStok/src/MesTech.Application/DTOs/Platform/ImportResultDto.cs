namespace MesTech.Application.DTOs.Platform;

public class ImportResultDto
{
    public int TotalProcessed { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess => Errors.Count == 0;
}
