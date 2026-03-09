namespace MesTech.Infrastructure.Integration.Invoice.Config;

/// <summary>BirFatura e-ticaret odakli fatura provider configuration. (D-06 Dalga 5)</summary>
public sealed class BirFaturaInvoiceConfig
{
    public const string Section = "Invoice:BirFatura";

    public string BaseUrl { get; set; } = "https://api.birfatura.com/v2/";
    public string ApiKey { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
}
