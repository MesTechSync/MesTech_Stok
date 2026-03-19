namespace MesTech.Application.DTOs.Crm;

public class CustomerCrmDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Segment { get; set; }
    public bool IsVip { get; set; }
    public bool IsActive { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
