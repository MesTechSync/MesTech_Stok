using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.AI;

/// <summary>
/// AI stok tahmini history — gerceklesen taleple karsilastirilarak dogruluk olculur.
/// </summary>
public class StockPrediction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public int PredictedDemand7d { get; set; }
    public int PredictedDemand14d { get; set; }
    public int PredictedDemand30d { get; set; }
    public int DaysUntilStockout { get; set; }
    public int ReorderSuggestion { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>7 gun sonra Hangfire job gerceklesen talebi yazar.</summary>
    public int? ActualDemand7d { get; set; }

    /// <summary>30 gun sonra Hangfire job gerceklesen talebi yazar.</summary>
    public int? ActualDemand30d { get; set; }
}
