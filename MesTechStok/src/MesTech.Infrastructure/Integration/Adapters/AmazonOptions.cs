using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for AmazonTrAdapter and AmazonEuAdapter.
/// Bind from appsettings.json section "Integrations:Amazon".
/// </summary>
public sealed class AmazonOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Amazon";

    /// <summary>Amazon Login With Amazon (LWA) OAuth2 token endpoint.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string LwaEndpoint { get; set; } = "https://api.amazon.com/auth/o2/token";

    /// <summary>Amazon SP-API EU region endpoint.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string EuEndpoint { get; set; } = "https://sellingpartnerapi-eu.amazon.com";

    /// <summary>Whether the Amazon integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>HTTP request timeout in seconds. Override via config for slow networks.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}
