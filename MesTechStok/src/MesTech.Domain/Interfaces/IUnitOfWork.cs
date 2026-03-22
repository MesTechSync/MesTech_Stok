namespace MesTech.Domain.Interfaces;

/// <summary>
/// Transaction yönetimi — SaveChanges + Domain Event dispatch.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Entity'yi DB'den yeniden yukler — concurrency retry icin.
    /// DbUpdateConcurrencyException sonrasi stale entity'yi tazeler.
    /// </summary>
    Task ReloadAsync<TEntity>(Guid id, CancellationToken ct = default) where TEntity : class;
}
