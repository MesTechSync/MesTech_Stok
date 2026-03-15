namespace MesTech.Application.DTOs.Crm;
public class DealDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = string.Empty;
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string? StageColor { get; set; }
    public Guid? CrmContactId { get; set; }
    public string? ContactName { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
