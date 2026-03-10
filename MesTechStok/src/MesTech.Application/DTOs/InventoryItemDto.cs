namespace MesTech.Application.DTOs;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime? LastMovement { get; set; }
}
