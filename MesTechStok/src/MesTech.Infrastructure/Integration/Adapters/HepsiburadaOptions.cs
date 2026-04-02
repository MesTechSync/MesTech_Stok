using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for HepsiburadaAdapter + HepsiburadaTokenService.
/// Bind from appsettings.json section "Integrations:Hepsiburada".
/// </summary>
public sealed class HepsiburadaOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Hepsiburada";

    /// <summary>Hepsiburada Merchant API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl { get; set; } = "https://mpop.hepsiburada.com";

    /// <summary>OAuth token endpoint URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string AuthUrl { get; set; } = "https://auth.hepsiburada.com/oauth/token";

    /// <summary>Whether the Hepsiburada integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>HTTP client timeout in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>Max concurrent API requests (SemaphoreSlim limit). Override via config.</summary>
    public int MaxConcurrentRequests { get; set; } = 20;
}
