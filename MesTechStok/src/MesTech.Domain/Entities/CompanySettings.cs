using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class CompanySettings : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
