using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.SyncBitrix24Contacts;

public class SyncBitrix24ContactsHandler
    : IRequestHandler<SyncBitrix24ContactsCommand, SyncBitrix24ContactsResult>
{
    private readonly IBitrix24ContactRepository _contactRepository;
    private readonly IBitrix24Adapter _adapter;
    private readonly IUnitOfWork _unitOfWork;

    public SyncBitrix24ContactsHandler(
        IBitrix24ContactRepository contactRepository,
        IBitrix24Adapter adapter,
        IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository ?? throw new ArgumentNullException(nameof(contactRepository));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SyncBitrix24ContactsResult> Handle(
        SyncBitrix24ContactsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var syncedCount = await _adapter.SyncContactsAsync(cancellationToken)
                .ConfigureAwait(false);

            // Mark unsynced contacts as synced
            var unsyncedContacts = await _contactRepository.GetUnsyncedAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var contact in unsyncedContacts)
            {
                contact.SyncStatus = SyncStatus.Synced;
                contact.LastSyncDate = DateTime.UtcNow;
                contact.SyncError = null;
                await _contactRepository.UpdateAsync(contact, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new SyncBitrix24ContactsResult
            {
                IsSuccess = true,
                SyncedCount = syncedCount
            };
        }
        catch (Exception ex)
        {
            return new SyncBitrix24ContactsResult
            {
                IsSuccess = false,
                ErrorCount = 1,
                Errors = { ex.Message }
            };
        }
    }
}
