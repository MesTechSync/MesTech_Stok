using MediatR;

namespace MesTech.Application.Features.Invoice.Queries.GetInvoiceSettings;

/// <summary>
/// Tenant-specific invoice settings query — provider, auto-numbering, tax defaults.
/// </summary>
public record GetInvoiceSettingsQuery(Guid TenantId) : IRequest<InvoiceSettingsDto>;

public sealed class InvoiceSettingsDto
{
    public string DefaultProvider { get; set; } = "None";
    public string DefaultScenario { get; set; } = "Basic";
    public string DefaultCurrency { get; set; } = "TRY";
    public decimal DefaultTaxRate { get; set; } = 0.20m;
    public string? InvoicePrefix { get; set; }
    public int NextInvoiceNumber { get; set; }
    public bool AutoApprove { get; set; }
    public bool AutoSendToGib { get; set; }
}
