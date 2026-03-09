namespace MesTech.Infrastructure.Integration.Invoice.Config;

/// <summary>Hepsiburada platform-dahili fatura provider configuration. (D-06 Dalga 5)</summary>
public sealed class HBFaturaInvoiceConfig
{
    public const string Section = "Invoice:HBFatura";

    public string BaseUrl { get; set; } = "https://listing-external.hepsiburada.com/";
    public string Username { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
