using MediatR;

namespace MesTech.Application.Features.Platform.Commands.UpdateStore;

public record UpdateStoreCommand(
    Guid StoreId,
    Guid TenantId,
    string StoreName,
    bool IsActive
) : IRequest<UpdateStoreResult>;

public sealed class UpdateStoreResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
