using MediatR;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// IScopedMediator implementation — her çağrıda yeni IServiceScope oluşturur.
/// Scope'lu IMediator resolve edilir ve çağrı sonrası scope dispose edilir.
///
/// Bu sayede her MediatR request kendi DbContext instance'ını alır —
/// concurrent access hatası ortadan kalkar.
///
/// NOT: WebApi'de HTTP request scope zaten bu işi yapar. Bu servis
/// SADECE desktop (Avalonia) ve background job senaryoları için gerekli.
/// </summary>
public sealed class ScopedMediator : IScopedMediator
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedMediator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request, ct).ConfigureAwait(false);
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(request, ct).ConfigureAwait(false);
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(notification, ct).ConfigureAwait(false);
    }
}
