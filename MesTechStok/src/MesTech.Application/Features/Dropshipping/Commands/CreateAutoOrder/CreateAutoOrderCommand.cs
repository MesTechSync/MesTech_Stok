using MediatR;

namespace MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;

/// <summary>
/// Minimum stok altındaki ürünler için otomatik dropship sipariş oluşturma komutu.
/// </summary>
public record CreateAutoOrderCommand(
    List<Guid> ProductIds,
    Guid SupplierId,
    bool AutoApprove = false
) : IRequest<AutoOrderResultDto>;

/// <summary>
/// Otomatik sipariş sonuç DTO'su.
/// </summary>
public class AutoOrderResultDto
{
    public int OrdersCreated { get; init; }
    public decimal TotalAmount { get; init; }
    public List<AutoOrderItemDto> Orders { get; init; } = [];
}

/// <summary>
/// Oluşturulan her sipariş kaydının özeti.
/// </summary>
public class AutoOrderItemDto
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int MinimumStock { get; init; }
}
