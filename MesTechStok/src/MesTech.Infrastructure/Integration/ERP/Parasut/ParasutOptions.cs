using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.ERP.Parasut;

/// <summary>
/// Configuration options for Parasut ERP adapter.
/// Bind from appsettings.json section "ERP:Parasut".
/// Secrets (ClientId, ClientSecret) must be stored in user-secrets or environment variables.
/// </summary>
public sealed class ParasutOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "ERP:Parasut";

    /// <summary>Parasut Production API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string ProductionBaseUrl { get; set; } = "https://api.parasut.com";

    /// <summary>Parasut Sandbox (staging/Heroku) API base URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string SandboxBaseUrl { get; set; } = "https://api.heroku.parasut.com";

    /// <summary>When true, routes all API calls through SandboxBaseUrl.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool UseSandbox { get; set; } = false;

    /// <summary>Resolved base URL — sandbox or production depending on UseSandbox.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl => UseSandbox ? SandboxBaseUrl : ProductionBaseUrl;

    /// <summary>Parasut OAuth2 token endpoint derived from the resolved BaseUrl.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string TokenUrl => $"{BaseUrl}/oauth/token";

    /// <summary>Parasut OAuth2 Client ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Parasut OAuth2 Client Secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Parasut company ID for API URL path: /v4/{CompanyId}/...</summary>
    public string CompanyId { get; set; } = string.Empty;
}
