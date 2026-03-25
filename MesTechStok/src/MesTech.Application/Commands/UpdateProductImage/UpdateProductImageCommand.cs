using MediatR;

namespace MesTech.Application.Commands.UpdateProductImage;

public record UpdateProductImageCommand(
    Guid ProductId,
    string ImageUrl
) : IRequest<UpdateProductImageResult>;

public sealed class UpdateProductImageResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
