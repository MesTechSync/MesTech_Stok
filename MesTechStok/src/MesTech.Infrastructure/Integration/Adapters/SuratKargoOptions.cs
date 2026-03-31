using System.Diagnostics.CodeAnalysis;

namespace MesTech.Infrastructure.Integration.Adapters;

public sealed class SuratKargoOptions
{
    public const string Section = "Integrations:SuratKargo";

    [SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Configuration binding requires string type.")]
    public string BaseUrl { get; set; } = "https://api.suratkargo.com.tr";

    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
        Justification = "Explicit false default documents intent for configuration binding.")]
    public bool Enabled { get; set; } = false;

    public int HttpTimeoutSeconds { get; set; } = 30;
}
