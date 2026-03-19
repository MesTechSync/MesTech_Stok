namespace MesTech.Application.DTOs.Platform;

public class DropshipProfitDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal CustomerPrice { get; set; }
    public decimal SupplierPrice { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
}
