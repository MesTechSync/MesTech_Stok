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
    public bool IsActive { get; private set; } = true;
    public bool IsReserved { get; private set; }
    public bool IsLocked { get; private set; }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void Reserve()
    {
        if (IsLocked) throw new InvalidOperationException("Kilitli bin rezerve edilemez.");
        IsReserved = true;
    }

    public void ReleaseReservation() => IsReserved = false;

    public void Lock()
    {
        IsLocked = true;
        IsReserved = false; // Kilit rezervasyonu iptal eder
    }

    public void Unlock() => IsLocked = false;

    public bool IsAvailable => IsActive && !IsReserved && !IsLocked;
}
