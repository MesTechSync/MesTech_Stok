using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for MngKargoAdapter.
/// Bind from appsettings.json section "Integrations:MngKargo".
/// </summary>
public sealed class MngKargoOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:MngKargo";

    /// <summary>MNG Kargo API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl { get; set; } = "https://apizone.mngkargo.com.tr/";

    /// <summary>Whether the MNG Kargo integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>HTTP client timeout in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}
