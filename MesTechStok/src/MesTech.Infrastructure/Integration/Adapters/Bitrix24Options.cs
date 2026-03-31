using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for Bitrix24Adapter.
/// Bind from appsettings.json section "Integrations:Bitrix24".
/// Portal domain is used to construct REST API URL: https://{PortalDomain}/rest/
/// </summary>
public sealed class Bitrix24Options
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Bitrix24";

    /// <summary>Bitrix24 portal domain (e.g. "mycompany.bitrix24.com").</summary>
    public string PortalDomain { get; set; } = string.Empty;

    /// <summary>Whether the Bitrix24 integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>HTTP client timeout in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}
