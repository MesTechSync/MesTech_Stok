using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

public sealed class PttKargoOptions
{
    public const string Section = "Integrations:PttKargo";

    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl { get; set; } = "https://pttws.ptt.gov.tr";

    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string ShipmentServiceUrl { get; set; } = "https://pttws.ptt.gov.tr/PttVeriYukleme/services/Sorgu";

    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string TrackingServiceUrl { get; set; } = "https://pttws.ptt.gov.tr/GonderiTakip/services/Sorgu";

    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    public int HttpTimeoutSeconds { get; set; } = 30;
}
