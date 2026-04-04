using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Security;

/// <summary>
/// SSRF (Server-Side Request Forgery) koruması — adapter URL'lerini doğrular.
/// Private/internal IP aralıklarına yönlenen URL'leri tespit eder ve loglar.
/// OWASP ASVS V13.1: SSRF Prevention.
/// </summary>
public static class SsrfGuard
{
    /// <summary>
    /// URL'nin private/internal ağa yönlenip yönlenmediğini kontrol eder.
    /// Yönleniyorsa warning log basar ve false döner (reject önerisi).
    /// </summary>
    public static bool ValidateUrl(string url, ILogger logger, string adapterName)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != "https" && parsedUri.Scheme != "http"))
        {
            logger.LogWarning("[{Adapter}] Invalid URL scheme: {Url}. Only HTTP(S) allowed.", adapterName, url);
            return false;
        }

        if (IsPrivateHost(parsedUri.Host))
        {
            logger.LogWarning(
                "[{Adapter}] URL points to private/internal network: {Url} — SSRF risk.",
                adapterName, url);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Host'un private/internal IP aralığında olup olmadığını kontrol eder.
    /// RFC 1918 + RFC 5735 + loopback + link-local kapsar.
    /// </summary>
    public static bool IsPrivateHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return true;

        // Loopback
        if (host is "localhost" or "127.0.0.1" or "::1" or "[::1]")
            return true;

        // RFC 1918 private ranges
        if (host.StartsWith("10.") ||
            host.StartsWith("192.168.") ||
            host.StartsWith("172.16.") || host.StartsWith("172.17.") ||
            host.StartsWith("172.18.") || host.StartsWith("172.19.") ||
            host.StartsWith("172.20.") || host.StartsWith("172.21.") ||
            host.StartsWith("172.22.") || host.StartsWith("172.23.") ||
            host.StartsWith("172.24.") || host.StartsWith("172.25.") ||
            host.StartsWith("172.26.") || host.StartsWith("172.27.") ||
            host.StartsWith("172.28.") || host.StartsWith("172.29.") ||
            host.StartsWith("172.30.") || host.StartsWith("172.31."))
            return true;

        // Link-local
        if (host.StartsWith("169.254."))
            return true;

        // Metadata endpoints (cloud SSRF)
        if (host is "metadata.google.internal" or "169.254.169.254")
            return true;

        return false;
    }
}
