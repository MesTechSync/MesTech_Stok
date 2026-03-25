namespace MesTech.Infrastructure.Integration.ERP.Nebim;

/// <summary>
/// Nebim V3 ERP API configuration options.
/// Config section: "ERP:Nebim"
/// </summary>
public sealed class NebimOptions
{
    public const string SectionName = "ERP:Nebim";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DatabaseCode { get; set; } = string.Empty;
    public string OfficeCode { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
}
