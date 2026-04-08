using MediatR;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Scoped MediatR dispatcher — her Send/Publish çağrısı yeni DI scope oluşturur.
/// Desktop app'te (Avalonia) root scope'dan resolve edilen DbContext concurrent
/// access hatası verir. Bu servis her çağrıda yeni scope → yeni DbContext sağlar.
///
/// KN-1 / G67 FIX: "A second operation was started on this context instance"
/// hatasını ortadan kaldırır.
///
/// KULLANIM: ViewModel'lerde IMediator yerine IScopedMediator inject edilir.
/// </summary>
public interface IScopedMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
    Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest;
    Task Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification;
}
