namespace MesTech.Domain.Exceptions;

public sealed class InsufficientStockException : DomainException
{
    public string SKU { get; }
    public int AvailableStock { get; }
    public int RequestedQuantity { get; }

    public InsufficientStockException(string sku, int availableStock, int requestedQuantity)
        : base($"Insufficient stock for SKU '{sku}': available={availableStock}, requested={requestedQuantity}")
    {
        SKU = sku;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }
}
