namespace MesTech.Application.DTOs.Dropshipping;

public class DropshipProductDto
{
    public Guid Id { get; set; }
    public Guid DropshipSupplierId { get; set; }
    public string ExternalProductId { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int StockQuantity { get; set; }
    public Guid? ProductId { get; set; }
    public bool IsLinked { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
