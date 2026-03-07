using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class WarehouseRack : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int ZoneId { get; set; }
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
    public bool IsActive { get; set; } = true;
}
