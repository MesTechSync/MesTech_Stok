namespace MesTech.Infrastructure.Integration.ERP.ERPNext;

/// <summary>
/// ERPNext connection configuration.
/// Frappe REST API: token-based auth (API Key + API Secret).
/// Auth header: Authorization: token {api_key}:{api_secret}
/// </summary>
public sealed class ERPNextOptions
{
    public const string Section = "ERP:ERPNext";

    /// <summary>ERPNext base URL (e.g. https://erp.mestech.app)</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Frappe API Key (from User → API Access)</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Frappe API Secret (from User → API Access)</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Default company name in ERPNext (for multi-company setups)</summary>
    public string Company { get; set; } = "MesTech";

    /// <summary>Default warehouse for stock entries</summary>
    public string DefaultWarehouse { get; set; } = "Stores - MT";

    /// <summary>Whether ERPNext integration is enabled</summary>
    public bool Enabled { get; set; }

    public bool IsConfigured => Enabled
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}
