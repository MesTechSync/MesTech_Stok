namespace MesTech.Domain.Services;

/// <summary>
/// FEFO toplama sonucu — hangi kalemden ne kadar alinacagi.
/// </summary>
public record FEFOPickResult(
    FEFOStockItem Item,
    decimal PickQuantity);
