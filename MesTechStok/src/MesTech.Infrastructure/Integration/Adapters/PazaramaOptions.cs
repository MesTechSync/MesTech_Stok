using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for PazaramaAdapter.
/// Bind from appsettings.json section "Integrations:Pazarama".
/// </summary>
public sealed class PazaramaOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Pazarama";

    /// <summary>Pazarama API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl { get; set; } = "https://isortagimgiris.pazarama.com";

    /// <summary>OAuth token endpoint URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string TokenUrl { get; set; } = "https://isortagimgiris.pazarama.com/connect/token";

    /// <summary>Whether the Pazarama integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>HTTP client timeout in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>Webhook HMAC-SHA256 dogrulama secret'i. Bos ise dogrulama atlaniyor.</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
