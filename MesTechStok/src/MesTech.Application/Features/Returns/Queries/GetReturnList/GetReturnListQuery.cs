using MediatR;

namespace MesTech.Application.Features.Returns.Queries.GetReturnList;

public record GetReturnListQuery(Guid TenantId, int Count = 100) : IRequest<IReadOnlyList<ReturnListItemDto>>;

public sealed class ReturnListItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal RefundAmount { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
