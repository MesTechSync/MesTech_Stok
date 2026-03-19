namespace MesTech.Application.Helpers;

/// <summary>
/// FIFO (First In First Out) maliyet hesaplama yardımcısı.
/// Lotları giriş tarihine göre sıralar, en eski lot'tan başlayarak tüketir.
/// </summary>
public static class FifoCalculator
{
    /// <summary>
    /// FIFO kuralına göre çıkış maliyetini hesaplar.
    /// </summary>
    /// <param name="lots">Mevcut lotlar (sırasız olabilir — metod içinde sıralanır).</param>
    /// <param name="quantityToSell">Satılacak / çıkış yapılacak adet.</param>
    /// <returns>Toplam FIFO maliyeti. Yetersiz stok durumunda sadece mevcut kadar hesaplar.</returns>
    public static decimal CalculateFifoCost(IReadOnlyList<FifoLotInput> lots, int quantityToSell)
    {
        if (lots is null || lots.Count == 0 || quantityToSell <= 0)
            return 0m;

        var sorted = lots.OrderBy(l => l.EntryDate).ToList();
        decimal totalCost = 0;
        int remaining = quantityToSell;

        foreach (var lot in sorted)
        {
            if (remaining <= 0) break;

            int fromThisLot = Math.Min(remaining, lot.Quantity);
            totalCost += fromThisLot * lot.UnitCost;
            remaining -= fromThisLot;
        }

        return totalCost;
    }

    /// <summary>
    /// FIFO sırasıyla lot tüketim planı döndürür.
    /// </summary>
    public static IReadOnlyList<FifoConsumptionPlan> GetConsumptionPlan(
        IReadOnlyList<FifoLotInput> lots, int quantityToSell)
    {
        if (lots is null || lots.Count == 0 || quantityToSell <= 0)
            return Array.Empty<FifoConsumptionPlan>();

        var sorted = lots.OrderBy(l => l.EntryDate).ToList();
        var plan = new List<FifoConsumptionPlan>();
        int remaining = quantityToSell;

        foreach (var lot in sorted)
        {
            if (remaining <= 0) break;

            int fromThisLot = Math.Min(remaining, lot.Quantity);
            plan.Add(new FifoConsumptionPlan(
                lot.LotNumber,
                fromThisLot,
                lot.UnitCost,
                fromThisLot * lot.UnitCost));
            remaining -= fromThisLot;
        }

        return plan;
    }
}

/// <summary>FIFO hesaplama girdi modeli.</summary>
public record FifoLotInput(
    string LotNumber,
    DateTime EntryDate,
    int Quantity,
    decimal UnitCost);

/// <summary>FIFO tüketim planı satırı.</summary>
public record FifoConsumptionPlan(
    string LotNumber,
    int ConsumedQuantity,
    decimal UnitCost,
    decimal TotalCost);
