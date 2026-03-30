using FluentValidation;

namespace MesTech.Application.Features.Invoice.Queries.GetInvoiceSettings;

public sealed class GetInvoiceSettingsValidator : AbstractValidator<GetInvoiceSettingsQuery>
{
    public GetInvoiceSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
