namespace MesTech.Application.DTOs.Shipping;

/// <summary>
/// Toplu kargo gonderim sonucu.
/// </summary>
public class BatchShipResult
{
    public int TotalOrders { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<AutoShipResult> Results { get; set; } = new();

    public static BatchShipResult Create(List<AutoShipResult> results)
        => new()
        {
            TotalOrders = results.Count,
            Successful = results.Count(r => r.Success),
            Failed = results.Count(r => !r.Success),
            Results = results
        };
}
