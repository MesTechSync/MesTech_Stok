using MediatR;

namespace MesTech.Application.Features.Billing.Commands.ProcessPaymentWebhook;

public record ProcessPaymentWebhookCommand(
    string Provider,
    string RawBody,
    string? Signature
) : IRequest<PaymentWebhookResult>;

public sealed class PaymentWebhookResult
{
    public bool Success { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? Error { get; init; }
    public Guid? SubscriptionId { get; init; }

    public static PaymentWebhookResult Ok(string eventType, Guid? subscriptionId = null)
        => new() { Success = true, EventType = eventType, SubscriptionId = subscriptionId };

    public static PaymentWebhookResult Fail(string error)
        => new() { Success = false, Error = error };
}
