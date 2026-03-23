using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for PttAvmAdapter.
/// Bind from appsettings.json section "Integrations:PttAvm".
/// </summary>
public sealed class PttAvmOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:PttAvm";

    /// <summary>PTT AVM API gateway base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl { get; set; } = "https://apigw.pttavm.com";

    /// <summary>PTT AVM authentication login endpoint.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string TokenEndpoint { get; set; } = "https://apigw.pttavm.com/api/auth/login";

    /// <summary>Whether the PTT AVM integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;
}
