namespace MesTech.Application.DTOs;

/// <summary>
/// Tenant data transfer object.
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; }
    public int StoreCount { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
