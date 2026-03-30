using FluentValidation;

namespace MesTech.Application.Features.Invoice.Queries;

public sealed class GetInvoiceReportValidator : AbstractValidator<GetInvoiceReportQuery>
{
    public GetInvoiceReportValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From)
            .WithMessage("Bitis tarihi baslangic tarihinden buyuk olmalidir.");
    }
}
