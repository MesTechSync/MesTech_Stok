using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.ExecuteBulkImport;

public class ExecuteBulkImportValidator : AbstractValidator<ExecuteBulkImportCommand>
{
    public ExecuteBulkImportValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500);
    }
}
