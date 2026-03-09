namespace MesTech.Infrastructure.Integration.Invoice.Config;

/// <summary>GIB Portal (devlet) e-Arsiv fatura provider configuration. (D-06 Dalga 5)</summary>
public sealed class GibPortalInvoiceConfig
{
    public const string Section = "Invoice:GibPortal";

    public string BaseUrl { get; set; } = "https://earsivportal.efatura.gov.tr/";
    public string VKN { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
