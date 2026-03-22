using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace MesTech.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IDomainEventDispatcher _dispatcher;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    public UnitOfWork(AppDbContext context, IDomainEventDispatcher dispatcher)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = _context.GetDomainEvents();
        var result = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (events.Count > 0)
            await _dispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);

        return result;
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

    protected virtual void Dispose(bool disposing)
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
