namespace MesTech.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? ListPrice { get; set; }
    public decimal TaxRate { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public int MaximumStock { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public string? Brand { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Computed
    public decimal ProfitMargin { get; set; }
    public decimal TotalValue { get; set; }
    public string StockStatus { get; set; } = string.Empty;
    public bool NeedsReorder { get; set; }
}
