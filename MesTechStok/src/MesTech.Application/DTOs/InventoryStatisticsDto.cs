namespace MesTech.Application.DTOs;

public class InventoryStatisticsDto
{
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int CriticalStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int TodayMovements { get; set; }
    public int TotalItems { get; set; }
}
