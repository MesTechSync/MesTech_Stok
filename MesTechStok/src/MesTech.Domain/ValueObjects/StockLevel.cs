namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Stok seviye bilgisi — Min/Max/Reorder kapsülleme.
/// </summary>
public record StockLevel(int Current, int Minimum, int Maximum, int ReorderLevel, int ReorderQuantity)
{
    public bool IsLow => Current <= Minimum;
    public bool IsCritical => ReorderLevel > 0 && Current <= ReorderLevel / 2;
    public bool NeedsReorder => Current <= ReorderLevel;
    public int ReorderAmount => NeedsReorder ? ReorderQuantity : 0;
    public bool IsOverStock => Maximum > 0 && Current > Maximum;
    public bool IsOutOfStock => Current <= 0;

    public string Status =>
        IsOutOfStock ? "OutOfStock" :
        IsCritical ? "Critical" :
        IsLow ? "Low" :
        IsOverStock ? "OverStock" :
        "Normal";
}
