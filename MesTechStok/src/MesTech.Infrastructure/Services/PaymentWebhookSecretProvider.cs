using MesTech.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MesTech.Infrastructure.Services;

public sealed class PaymentWebhookSecretProvider : IPaymentWebhookSecretProvider
{
    private readonly IConfiguration _configuration;

    public PaymentWebhookSecretProvider(IConfiguration configuration)
        => _configuration = configuration;

    public string? GetSecret(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "stripe" => _configuration["Stripe:WebhookSecret"],
            "iyzico" => _configuration["Iyzico:WebhookSecret"],
            _ => null
        };
    }
}
