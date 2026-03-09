namespace MesTech.Infrastructure.Integration.Invoice.Config;

/// <summary>E-Logo (EDM Bilisim) e-Fatura provider configuration. (D-06 Dalga 5)</summary>
public sealed class ELogoInvoiceConfig
{
    public const string Section = "Invoice:ELogo";

    public string BaseUrl { get; set; } = "https://efatura-api.edmbilisim.com/v1/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VKN { get; set; } = string.Empty;
}
