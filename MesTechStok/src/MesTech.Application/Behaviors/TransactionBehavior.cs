using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — Command'ları transaction içinde çalıştırır.
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        ArgumentNullException.ThrowIfNull(next);

        // Sadece Command'lar için transaction aç (Query'ler için değil)
        var isCommand = typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal);
        if (!isCommand) return await next().ConfigureAwait(false);

        await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var response = await next().ConfigureAwait(false);
            await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
