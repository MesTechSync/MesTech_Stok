using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for ZalandoAdapter.
/// Bind from appsettings.json section "Integrations:Zalando".
/// Secrets (ClientId, ClientSecret) must be stored in user-secrets or environment variables.
/// </summary>
public sealed class ZalandoOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Zalando";

    /// <summary>OAuth2 Client ID (partner application credential).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth2 Client Secret (partner application credential).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>When true, the Zalando integration is active.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    /// <summary>OAuth2 token endpoint URL.</summary>
    public string TokenUrl { get; set; } = "https://auth.zalando.com/oauth2/access_token";

    /// <summary>Zalando Partner API base URL.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.zalando.com";

    /// <summary>HTTP client timeout in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}
