using MediatR;

namespace MesTech.Application.Features.Invoice.Queries.GetInvoiceSettings;

public sealed class GetInvoiceSettingsHandler : IRequestHandler<GetInvoiceSettingsQuery, InvoiceSettingsDto>
{
    public Task<InvoiceSettingsDto> Handle(GetInvoiceSettingsQuery request, CancellationToken cancellationToken)
    {
        // Defaults for Turkish e-commerce — will wire to ICompanySettingsRepository when persisted
        var result = new InvoiceSettingsDto
        {
            DefaultProvider = "None",
            DefaultScenario = "Basic",
            DefaultCurrency = "TRY",
            DefaultTaxRate = 0.20m,
            InvoicePrefix = "INV",
            NextInvoiceNumber = 1,
            AutoApprove = false,
            AutoSendToGib = false
        };

        return Task.FromResult(result);
    }
}
