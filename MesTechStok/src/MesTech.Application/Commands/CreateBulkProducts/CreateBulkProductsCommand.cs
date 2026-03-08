using MediatR;

namespace MesTech.Application.Commands.CreateBulkProducts;

public record CreateBulkProductsCommand(int Count = 40) : IRequest<CreateBulkProductsResult>;

public class CreateBulkProductsResult
{
    public bool IsSuccess { get; set; }
    public int CreatedCount { get; set; }
    public string? Message { get; set; }
}
