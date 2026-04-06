namespace MesTech.Application.DTOs;

/// <summary>
/// Stock Movement data transfer object.
/// </summary>
public sealed class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSKU { get; set; }
    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime Date { get; set; }
    public string? ProcessedBy { get; set; }
    public bool IsApproved { get; set; }
    public string? Warehouse { get; set; }
}
