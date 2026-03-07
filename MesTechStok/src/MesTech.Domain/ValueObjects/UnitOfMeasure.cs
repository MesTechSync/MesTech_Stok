namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Birim dönüşümleri value object.
/// </summary>
public record UnitOfMeasure(string Unit, decimal Quantity = 1)
{
    public static UnitOfMeasure Piece(decimal qty = 1) => new("PCS", qty);
    public static UnitOfMeasure Kilogram(decimal qty = 1) => new("KG", qty);
    public static UnitOfMeasure Gram(decimal qty = 1) => new("GR", qty);
    public static UnitOfMeasure Liter(decimal qty = 1) => new("LT", qty);
    public static UnitOfMeasure Meter(decimal qty = 1) => new("MT", qty);
    public static UnitOfMeasure Box(decimal qty = 1) => new("BOX", qty);
    public static UnitOfMeasure Pallet(decimal qty = 1) => new("PLT", qty);

    public override string ToString() => $"{Quantity} {Unit}";
}
