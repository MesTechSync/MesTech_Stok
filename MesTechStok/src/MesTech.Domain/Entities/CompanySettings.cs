using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public sealed class CompanySettings : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
