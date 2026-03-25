namespace MesTech.Application.DTOs.Dropshipping;

/// <summary>
/// Dropship Order data transfer object.
/// </summary>
public sealed class DropshipOrderDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid DropshipSupplierId { get; set; }
    public Guid DropshipProductId { get; set; }
    public string? SupplierOrderRef { get; set; }
    public string? SupplierTrackingNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime? OrderedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
