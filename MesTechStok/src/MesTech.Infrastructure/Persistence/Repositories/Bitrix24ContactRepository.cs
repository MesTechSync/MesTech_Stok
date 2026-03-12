using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class Bitrix24ContactRepository : IBitrix24ContactRepository
{
    private readonly AppDbContext _context;

    public Bitrix24ContactRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Bitrix24Contact?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Bitrix24Contacts.FindAsync([id], ct).ConfigureAwait(false);

    public async Task<Bitrix24Contact?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await _context.Bitrix24Contacts
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct).ConfigureAwait(false);

    public async Task<Bitrix24Contact?> GetByExternalContactIdAsync(string externalContactId, CancellationToken ct = default)
        => await _context.Bitrix24Contacts
            .FirstOrDefaultAsync(c => c.ExternalContactId == externalContactId, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Bitrix24Contact>> GetUnsyncedAsync(CancellationToken ct = default)
        => await _context.Bitrix24Contacts
            .Where(c => c.SyncStatus == SyncStatus.NotSynced)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Bitrix24Contact contact, CancellationToken ct = default)
        => await _context.Bitrix24Contacts.AddAsync(contact, ct).ConfigureAwait(false);

    public Task UpdateAsync(Bitrix24Contact contact, CancellationToken ct = default)
    {
        _context.Bitrix24Contacts.Update(contact);
        return Task.CompletedTask;
    }
}
