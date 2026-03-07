namespace MesTech.Application.DTOs;

public class TenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; }
    public int StoreCount { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
