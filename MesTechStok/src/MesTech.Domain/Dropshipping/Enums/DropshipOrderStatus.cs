namespace MesTech.Domain.Dropshipping.Enums;

/// <summary>
/// Dropship sipariş durumu. Tedarikçiye sipariş oluşturulduktan teslimat tamamlanana kadar.
/// </summary>
public enum DropshipOrderStatus
{
    Pending,
    OrderedFromSupplier,
    Shipped,
    Delivered,
    Failed
}
