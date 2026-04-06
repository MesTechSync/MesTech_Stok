using MediatR;

namespace MesTech.Application.Features.Platform.Commands.DeleteStore;

public record DeleteStoreCommand(
    Guid StoreId,
    Guid TenantId
) : IRequest<DeleteStoreResult>;

public sealed class DeleteStoreResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
