using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class WarehouseBin : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid ShelfId { get; set; }
    public int BinNumber { get; set; }
    public decimal? Width { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Height { get; set; }
    public decimal? Volume { get; set; }
    public int? XPosition { get; set; }
    public int? YPosition { get; set; }
    public int? ZPosition { get; set; }
    public string? BinType { get; set; }
    public decimal? MaxWeight { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsReserved { get; set; }
    public bool IsLocked { get; set; }
}
