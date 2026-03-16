using MediatR;

namespace MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;

/// <summary>
/// Magaza credential'larini soft-delete eder.
/// BaseEntity.IsDeleted = true, DeletedAt = UtcNow olarak isaretlenir.
/// </summary>
public record DeleteStoreCredentialCommand(
    Guid StoreId,
    string DeletedBy = "system"
) : IRequest<bool>;
