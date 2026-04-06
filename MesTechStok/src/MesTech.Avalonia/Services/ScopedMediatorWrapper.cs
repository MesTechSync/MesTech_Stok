using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Avalonia.Services;

/// <summary>
/// IMediator wrapper that creates a NEW DI scope for each Send/Publish call.
/// Resolves the concurrent DbContext access crash in Avalonia desktop app.
///
/// KN-1 / G67 FIX: Each MediatR operation gets its own DbContext instance,
/// preventing "A second operation was started on this context instance" errors.
///
/// This wrapper is registered in Avalonia DI AFTER AddMediatR() —
/// it overrides the default IMediator registration. All existing ViewModels
/// that inject IMediator automatically get scoped behavior without code changes.
/// </summary>
public sealed class ScopedMediatorWrapper : IMediator
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedMediatorWrapper(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mediator>();
        return await mediator.Send(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mediator>();
        await mediator.Send(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mediator>();
        return await mediator.Send(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mediator>();
        await mediator.Publish(notification, cancellationToken).ConfigureAwait(false);
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mediator>();
        await mediator.Publish(notification, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Stream requests are rare — delegate to root mediator
        // Creating scope per-item would be too expensive
        throw new NotSupportedException("Streaming is not supported through ScopedMediatorWrapper. Use IMediator directly for stream requests.");
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming is not supported through ScopedMediatorWrapper.");
    }
}
