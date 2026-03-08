using MediatR;

namespace MesTech.Application.Queries.GetProductDbStatus;

public record GetProductDbStatusQuery() : IRequest<ProductDbStatusDto>;

public class ProductDbStatusDto
{
    public bool IsConnected { get; set; }
    public int ActiveCount { get; set; }
    public int TotalCount { get; set; }
}
