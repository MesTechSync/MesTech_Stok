namespace MesTech.Application.Interfaces;

/// <summary>
/// Payment provider webhook secret provider — Stripe, Iyzico vb. webhook signature doğrulaması için.
/// </summary>
public interface IPaymentWebhookSecretProvider
{
    string? GetSecret(string provider);
}
