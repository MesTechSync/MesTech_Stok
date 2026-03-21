namespace MesTech.Application.DTOs.Tasks;

/// <summary>
/// Project data transfer object.
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
}
