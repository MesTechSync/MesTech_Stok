using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.ExecuteBulkImport;

public sealed class ExecuteBulkImportValidator : AbstractValidator<ExecuteBulkImportCommand>
{
    public ExecuteBulkImportValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500);
    }
}
