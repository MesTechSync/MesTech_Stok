using FluentValidation;

namespace MesTech.Application.Features.Invoice.Queries;

public sealed class GetInvoiceProvidersValidator : AbstractValidator<GetInvoiceProvidersQuery>
{
    public GetInvoiceProvidersValidator()
    {
        // Parameterless query — no validation rules needed.
    }
}
