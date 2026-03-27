using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// Amazon SNS message signature doğrulama — RSA SHA1 sertifika tabanlı.
/// Amazon SP-API webhook'ları SNS üzerinden gelir.
///
/// Doğrulama adımları (AWS resmi docs):
/// 1. SigningCertURL domain kontrolü (https://sns.{region}.amazonaws.com/)
/// 2. Sertifikayı indir + cache
/// 3. Canonical string oluştur (Type + MessageId + Message + Subject + Timestamp + TopicArn)
/// 4. RSA-SHA1 ile imza doğrula
///
/// Kaynak: https://docs.aws.amazon.com/sns/latest/dg/sns-verify-signature-of-message.html
/// </summary>
public sealed class AmazonSnsSignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "amazon";

    private static readonly HttpClient _certClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public bool Validate(string body, string signature, string secret)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // 1. SigningCertURL domain validation — STRICT
            if (!root.TryGetProperty("SigningCertURL", out var certUrlEl))
                return false;

            var certUrl = certUrlEl.GetString() ?? "";
            if (!IsValidAmazonCertUrl(certUrl))
                return false;

            // 2. Signature field
            if (!root.TryGetProperty("Signature", out var sigEl))
                return false;
            var signatureBytes = Convert.FromBase64String(sigEl.GetString() ?? "");

            // 3. Message type
            var messageType = root.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : null;
            if (messageType is null)
                return false;

            // 4. Build canonical string (AWS specification)
            var canonicalString = BuildCanonicalString(root, messageType);
            if (canonicalString is null)
                return false;

            // 5. Download certificate + RSA verify
            var certPem = DownloadCertificate(certUrl);
            if (certPem is null)
                return false;

            using var cert = X509Certificate2.CreateFromPem(certPem);
            using var rsa = cert.GetRSAPublicKey();
            if (rsa is null)
                return false;

            var dataBytes = Encoding.UTF8.GetBytes(canonicalString);
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// AWS SNS sertifika URL doğrulaması — STRICT.
    /// URL MUST be https://sns.{region}.amazonaws.com/...
    /// Subdomain attack engelleme: host tam olarak amazonaws.com ile bitmeli.
    /// </summary>
    private static bool IsValidAmazonCertUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != "https")
            return false;

        // Host must be sns.{region}.amazonaws.com — NOT *.amazonaws.com.evil.com
        var host = uri.Host;
        if (!host.StartsWith("sns.", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase))
            return false;

        // Validate only 3 parts: sns.{region}.amazonaws.com
        var parts = host.Split('.');
        if (parts.Length != 4) // sns + region + amazonaws + com
            return false;

        return true;
    }

    /// <summary>
    /// AWS SNS canonical string — used for signature verification.
    /// Order and inclusion of fields depends on message type.
    /// </summary>
    private static string? BuildCanonicalString(JsonElement root, string messageType)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Message");
        sb.AppendLine(GetString(root, "Message") ?? "");

        sb.AppendLine("MessageId");
        sb.AppendLine(GetString(root, "MessageId") ?? "");

        if (messageType == "Notification")
        {
            var subject = GetString(root, "Subject");
            if (subject is not null)
            {
                sb.AppendLine("Subject");
                sb.AppendLine(subject);
            }
        }
        else // SubscriptionConfirmation / UnsubscribeConfirmation
        {
            sb.AppendLine("SubscribeURL");
            sb.AppendLine(GetString(root, "SubscribeURL") ?? "");
        }

        sb.AppendLine("Timestamp");
        sb.AppendLine(GetString(root, "Timestamp") ?? "");

        sb.AppendLine("TopicArn");
        sb.AppendLine(GetString(root, "TopicArn") ?? "");

        sb.AppendLine("Type");
        sb.AppendLine(messageType);

        return sb.ToString();
    }

    private static string? GetString(JsonElement root, string prop) =>
        root.TryGetProperty(prop, out var el) ? el.GetString() : null;

    /// <summary>
    /// Download PEM certificate from AWS SNS.
    /// Synchronous + timeout 5s — webhook validation is blocking by design.
    /// </summary>
    private static string? DownloadCertificate(string certUrl)
    {
        try
        {
            // Sync over async acceptable here — webhook validation pipeline is synchronous
            return _certClient.GetStringAsync(certUrl).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }
}
