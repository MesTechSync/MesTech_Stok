using MesTech.Domain.Exceptions;
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
    /// SaveChanges + domain event dispatch.
    ///
    /// DEV6-TUR18 FIX (G039 P0): Retry loop KALDIRILDI.
    /// NEDEN: ReloadAsync handler'ın domain mutation'ını (AdjustStock, Renew vb.) geri alır.
    /// Retry sonrası SaveChanges boş diff kaydeder → veri KAYBI.
    ///
    /// Concurrency conflict → DbUpdateConcurrencyException fırlatılır.
    /// Handler seviyesinde retry gerekiyorsa handler kendisi tekrar çağrılmalı
    /// (tam mutation cycle: load → mutate → save).
    ///
    /// RowVersion koruması + Distributed Lock (TUR 12) birlikte çalışır:
    /// Lock → aynı anda sadece 1 handler çalışır → concurrency exception nadir.
    /// Lock alınamazsa handler zaten çalışmaz (stock deduction guard).
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = _context.GetDomainEvents();

        int result;
        try
        {
            result = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entityName = ex.Entries.Count > 0 ? ex.Entries[0].Entity.GetType().Name : "Unknown";
            _logger.LogWarning(ex, "Concurrency conflict on {Entity}", entityName);
            throw new ConcurrencyConflictException(entityName,
                "Kayıt başka bir kullanıcı tarafından değiştirildi. Lütfen sayfayı yenileyip tekrar deneyin.");
        }

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
        // FirstOrDefaultAsync respects global query filters (tenant isolation)
        // FindAsync bypasses them — security risk in multi-tenant context
        var entity = await _context.Set<TEntity>()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, ct).ConfigureAwait(false);
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
