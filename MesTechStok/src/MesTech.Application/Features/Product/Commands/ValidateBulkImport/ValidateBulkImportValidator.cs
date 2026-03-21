using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.ValidateBulkImport;

public class ValidateBulkImportValidator : AbstractValidator<ValidateBulkImportCommand>
{
    public ValidateBulkImportValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500);
    }
}
