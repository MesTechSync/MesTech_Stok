namespace MesTech.Domain.Exceptions;

/// <summary>
/// Optimistic concurrency çakışması — entity başka bir kullanıcı tarafından değiştirilmiş.
/// UnitOfWork'te DbUpdateConcurrencyException yakalanıp bu exception'a çevrilir.
/// Application katmanı EF bağımlılığı olmadan concurrency hatası yakalayabilir.
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string entityName, string message)
        : base($"Concurrency conflict on {entityName}: {message}")
    {
        EntityName = entityName;
    }

    public string EntityName { get; }
}
