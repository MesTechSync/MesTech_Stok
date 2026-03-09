namespace MesTech.Infrastructure.Middleware;

/// <summary>
/// Configuration for API Key authentication middleware.
/// Loaded from "ApiSecurity" config section. (IP-6 Dalga 5)
/// </summary>
public sealed class ApiKeyOptions
{
    public const string Section = "ApiSecurity";

    /// <summary>Header name to check. Default: X-API-Key</summary>
    public string HeaderName { get; set; } = "X-API-Key";

    /// <summary>Valid API keys loaded from user-secrets / environment variables. Never hardcode.</summary>
    public string[] ValidApiKeys { get; set; } = [];

    /// <summary>Paths that bypass API key check.</summary>
    public string[] BypassPaths { get; set; } = ["/health", "/metrics", "/api/mesa/status"];
}
