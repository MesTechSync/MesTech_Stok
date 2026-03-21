namespace MesTech.Application.DTOs.Crm;

/// <summary>
/// Lead data transfer object.
/// </summary>
public class LeadDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public DateTime? ContactedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
