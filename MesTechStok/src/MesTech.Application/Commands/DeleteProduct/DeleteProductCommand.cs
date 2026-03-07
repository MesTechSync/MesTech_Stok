using MediatR;

namespace MesTech.Application.Commands.DeleteProduct;

public record DeleteProductCommand(Guid ProductId) : IRequest<DeleteProductResult>;

public class DeleteProductResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
