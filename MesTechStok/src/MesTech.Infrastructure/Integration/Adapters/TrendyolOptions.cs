using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for TrendyolAdapter.
/// Bind from appsettings.json section "Integrations:Trendyol".
/// </summary>
public sealed class TrendyolOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Trendyol";

    /// <summary>Trendyol Production API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string ProductionBaseUrl { get; set; } = "https://apigw.trendyol.com";

    /// <summary>Trendyol Sandbox (stage) API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string SandboxBaseUrl { get; set; } = "https://stage-apigw.trendyol.com";

    /// <summary>When true, routes all API calls through SandboxBaseUrl.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool UseSandbox { get; set; } = false;

    /// <summary>Resolved base URL — sandbox or production depending on UseSandbox.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl => UseSandbox ? SandboxBaseUrl : ProductionBaseUrl;

    /// <summary>Whether the Trendyol integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// HTTP request timeout in seconds. Override via config for slow networks.
    /// 20s = 11s (max RetryAfter) + 5s (slow request margin) + 4s (safety buffer).
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 20;
}
