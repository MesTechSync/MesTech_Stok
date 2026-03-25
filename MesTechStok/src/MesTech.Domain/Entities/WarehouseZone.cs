using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public sealed class WarehouseZone : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public decimal? Height { get; set; }
    public decimal? Area { get; set; }
    public int? FloorNumber { get; set; }
    public string? BuildingSection { get; set; }
    public bool HasClimateControl { get; set; }
    public bool HasSecurity { get; set; }
    public string? TemperatureRange { get; set; }
    public string? HumidityRange { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; private set; } = true;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
