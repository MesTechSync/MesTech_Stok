using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs;

public class StoreDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public PlatformType PlatformType { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? ExternalStoreId { get; set; }
    public bool IsActive { get; set; }
    public int ProductMappingCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
