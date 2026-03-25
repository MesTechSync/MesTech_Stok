using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public sealed class WarehouseRack : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid ZoneId { get; set; }
    public decimal? Width { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Height { get; set; }
    public int ShelfCount { get; set; }
    public int BinCount { get; set; }
    public int? RowNumber { get; set; }
    public int? ColumnNumber { get; set; }
    public string? Orientation { get; set; }
    public string? RackType { get; set; }
    public decimal? MaxWeight { get; set; }
    public bool IsMovable { get; set; }
    public bool IsActive { get; private set; } = true;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
