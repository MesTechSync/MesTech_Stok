namespace MesTech.Infrastructure.Integration.Invoice.Config;

/// <summary>Dijital Planet GIB Ozel Entegrator provider configuration. (D-06 Dalga 5)</summary>
public sealed class DijiitalPlanetInvoiceConfig
{
    public const string Section = "Invoice:DijiitalPlanet";

    public string BaseUrl { get; set; } = "https://api.dijitalplanet.com.tr/efatura/";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
