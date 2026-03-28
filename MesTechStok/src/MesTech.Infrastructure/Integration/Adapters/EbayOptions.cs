using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for EbayAdapter.
/// Bind from appsettings.json section "Integrations:eBay".
/// </summary>
public sealed class EbayOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:eBay";

    /// <summary>eBay Production REST API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string ProductionBaseUrl { get; set; } = "https://api.ebay.com";

    /// <summary>eBay Sandbox REST API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string SandboxBaseUrl { get; set; } = "https://api.sandbox.ebay.com";

    /// <summary>When true, routes all API calls through SandboxBaseUrl.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool UseSandbox { get; set; } = false;

    /// <summary>Resolved base URL — sandbox or production depending on UseSandbox.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl => UseSandbox ? SandboxBaseUrl : ProductionBaseUrl;

    /// <summary>OAuth2 token endpoint derived from the resolved BaseUrl.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string TokenUrl => $"{BaseUrl}/identity/v1/oauth2/token";

    /// <summary>OAuth2 scope for eBay public API access.</summary>
    public string OAuthScope => $"{ProductionBaseUrl}/oauth/api_scope";

    /// <summary>Whether the eBay integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>eBay marketplace identifier (e.g. EBAY_TR, EBAY_US).</summary>
    public string MarketplaceId { get; set; } = "EBAY_TR";

    /// <summary>HTTP client timeout in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}
