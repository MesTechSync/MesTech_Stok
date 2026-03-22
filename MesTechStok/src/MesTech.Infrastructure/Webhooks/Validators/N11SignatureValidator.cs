using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// N11 webhook imza dogrulama — SHA512 HMAC base64.
/// N11 SOAP tabanli oldugu icin webhook destegi sinirli.
/// Bu validator REST webhook senaryosu icin hazir —
/// polling modunda (Hangfire) HMAC kontrolu gerekmez.
/// </summary>
public sealed class N11SignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "n11";

    public bool Validate(string body, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            return false;

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        var computed = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature));
    }
}
