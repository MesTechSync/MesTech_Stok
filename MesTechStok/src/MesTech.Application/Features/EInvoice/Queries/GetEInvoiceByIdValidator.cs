using FluentValidation;

namespace MesTech.Application.Features.EInvoice.Queries;

public sealed class GetEInvoiceByIdValidator : AbstractValidator<GetEInvoiceByIdQuery>
{
    public GetEInvoiceByIdValidator()
    {
        RuleFor(x => x.EInvoiceId)
            .NotEqual(Guid.Empty).WithMessage("Fatura kimliği boş olamaz.");
    }
}
