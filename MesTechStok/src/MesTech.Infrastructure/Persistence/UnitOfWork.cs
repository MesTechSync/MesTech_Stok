using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;
    private const int MaxConcurrencyRetries = 3;

    public UnitOfWork(AppDbContext context, IDomainEventDispatcher dispatcher, ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// SaveChanges with automatic concurrency exception retry (DEV6-TUR12).
    /// DbUpdateConcurrencyException → reload stale entries → retry (max 3 attempts).
    /// Prevents silent lost-update bugs on RowVersion-protected entities.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = _context.GetDomainEvents();

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            try
            {
                var result = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                if (events.Count > 0)
                    await _dispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxConcurrencyRetries)
            {
                _logger.LogWarning(
                    "Concurrency conflict detected (attempt {Attempt}/{Max}): {Message}. Reloading stale entries...",
                    attempt, MaxConcurrencyRetries, ex.Message);

                // Reload all stale entries from database
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Final attempt — let exception propagate if still conflicting
        var finalResult = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (events.Count > 0)
            await _dispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);
        return finalResult;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await _currentTransaction.DisposeAsync().ConfigureAwait(false);
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await _currentTransaction.DisposeAsync().ConfigureAwait(false);
            _currentTransaction = null;
        }
    }

    public async Task ReloadAsync<TEntity>(Guid id, CancellationToken ct = default) where TEntity : class
    {
        var entity = await _context.Set<TEntity>().FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (entity is not null)
            await _context.Entry(entity).ReloadAsync(ct).ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
        }

        _disposed = true;
    }
}
