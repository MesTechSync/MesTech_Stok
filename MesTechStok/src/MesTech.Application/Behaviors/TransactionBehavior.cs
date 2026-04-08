using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — Command'ları çalıştırır.
/// NOT: EnableRetryOnFailure aktif olduğunda explicit BeginTransaction kullanılamaz
/// (NpgsqlRetryingExecutionStrategy bunu reddeder). SaveChangesAsync zaten
/// kendi implicit transaction'ını açar — ayrıca explicit transaction gereksiz.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        ArgumentNullException.ThrowIfNull(next);

        // SaveChangesAsync kendi implicit transaction'ını açar.
        // Explicit transaction NpgsqlRetryingExecutionStrategy ile çakışır.
        return next();
    }
}
