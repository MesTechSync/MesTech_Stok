using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for N11Adapter.
/// Bind from appsettings.json section "Integrations:N11".
/// </summary>
public sealed class N11Options
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:N11";

    /// <summary>N11 API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl { get; set; } = "https://api.n11.com";

    /// <summary>Whether the N11 integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>HTTP client timeout in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>Webhook HMAC-SHA256 dogrulama secret'i. Bos ise dogrulama atlaniyor.</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
