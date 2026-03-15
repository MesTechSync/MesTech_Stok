namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// FIFO maliyet katmani — alis partisi bilgisi.
/// </summary>
public record CostLayerInput(int Quantity, decimal UnitCost);
