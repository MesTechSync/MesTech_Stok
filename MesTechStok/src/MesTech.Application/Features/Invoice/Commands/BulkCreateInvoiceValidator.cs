using FluentValidation;

namespace MesTech.Application.Features.Invoice.Commands;

public class BulkCreateInvoiceValidator : AbstractValidator<BulkCreateInvoiceCommand>
{
    public BulkCreateInvoiceValidator()
    {
        // No properties to validate — add rules as business requirements emerge
    }
}
