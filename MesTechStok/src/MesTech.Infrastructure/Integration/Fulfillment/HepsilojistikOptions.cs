using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Fulfillment;

/// <summary>
/// Configuration options for HepsilojistikAdapter.
/// Bind from appsettings.json section "Integrations:Hepsilojistik".
/// Note: Base URL is provisional — confirm with Hepsilojistik developer portal.
/// </summary>
public sealed class HepsilojistikOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Hepsilojistik";

    /// <summary>Hepsilojistik API base URL (provisional).</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl { get; set; } = "https://lojistik-api.hepsiburada.com/v1";

    /// <summary>Whether the Hepsilojistik integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;
}
