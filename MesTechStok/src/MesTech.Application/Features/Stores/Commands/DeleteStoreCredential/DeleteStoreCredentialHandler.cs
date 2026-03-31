using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;

public sealed class DeleteStoreCredentialHandler : IRequestHandler<DeleteStoreCredentialCommand, bool>
{
    private readonly IStoreCredentialRepository _credentialRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeleteStoreCredentialHandler> _logger;

    public DeleteStoreCredentialHandler(
        IStoreCredentialRepository credentialRepository,
        IUnitOfWork uow,
        ILogger<DeleteStoreCredentialHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteStoreCredentialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credentials = await _credentialRepository.GetByStoreIdAsync(request.StoreId, cancellationToken).ConfigureAwait(false);
        if (credentials.Count == 0)
            return false;

        foreach (var cred in credentials)
        {
            cred.IsDeleted = true;
            cred.DeletedAt = DateTime.UtcNow;
            cred.DeletedBy = request.DeletedBy;
            await _credentialRepository.UpdateAsync(cred, cancellationToken).ConfigureAwait(false);
        }

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Soft-deleted {Count} credential(s) for Store {StoreId}",
            credentials.Count, request.StoreId);

        return true;
    }
}
