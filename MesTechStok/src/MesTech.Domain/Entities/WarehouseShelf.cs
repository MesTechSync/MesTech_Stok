using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class WarehouseShelf : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid RackId { get; set; }
    public int LevelNumber { get; set; }
    public decimal? Height { get; set; }
    public decimal? MaxWeight { get; set; }
    public decimal? DistanceFromGround { get; set; }
    public string? Accessibility { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAccessible { get; set; } = true;
}
