using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Configuration options for YurticiKargoAdapter.
/// Bind from appsettings.json section "Integrations:YurticiKargo".
/// </summary>
public sealed class YurticiKargoOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:YurticiKargo";

    /// <summary>Yurtici Kargo Production SOAP web service URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string ProductionServiceUrl { get; set; } = "https://webservices.yurticikargo.com/ShippingOrderDispatcherServices/ws";

    /// <summary>Yurtici Kargo Test/Sandbox SOAP web service URL.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string SandboxServiceUrl { get; set; } = "https://testwebservices.yurticikargo.com/ShippingOrderDispatcherServices/ws";

    /// <summary>When true, routes all SOAP calls through SandboxServiceUrl.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool UseSandbox { get; set; } = false;

    /// <summary>Resolved service URL — sandbox or production depending on UseSandbox.</summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string ServiceUrl => UseSandbox ? SandboxServiceUrl : ProductionServiceUrl;

    /// <summary>Whether the Yurtici Kargo integration is enabled.</summary>
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;
}
