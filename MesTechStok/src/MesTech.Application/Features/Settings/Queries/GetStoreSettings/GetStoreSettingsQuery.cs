using MediatR;

namespace MesTech.Application.Features.Settings.Queries.GetStoreSettings;

public record GetStoreSettingsQuery(Guid TenantId) : IRequest<StoreSettingsDto>;

public sealed class StoreSettingsDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public List<StoreInfoDto> Stores { get; set; } = new();
}

public sealed class StoreInfoDto
{
    public Guid StoreId { get; set; }
    public string PlatformType { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool HasCredentials { get; set; }
}
