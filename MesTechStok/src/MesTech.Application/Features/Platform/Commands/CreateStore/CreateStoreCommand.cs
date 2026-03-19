using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Platform.Commands.CreateStore;

public record CreateStoreCommand(
    Guid TenantId,
    string StoreName,
    PlatformType PlatformType,
    Dictionary<string, string> Credentials
) : IRequest<CreateStoreResult>;

public class CreateStoreResult
{
    public bool IsSuccess { get; set; }
    public Guid? StoreId { get; set; }
    public string? ErrorMessage { get; set; }
}
