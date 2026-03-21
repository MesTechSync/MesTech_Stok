using FluentValidation;

namespace MesTech.Application.Commands.SeedDemoData;

public class SeedDemoDataValidator : AbstractValidator<SeedDemoDataCommand>
{
    public SeedDemoDataValidator()
    {
        // No properties to validate — add rules as business requirements emerge
    }
}
