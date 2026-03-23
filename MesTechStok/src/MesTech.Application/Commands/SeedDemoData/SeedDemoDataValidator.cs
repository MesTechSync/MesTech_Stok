using FluentValidation;

namespace MesTech.Application.Commands.SeedDemoData;

public class SeedDemoDataValidator : AbstractValidator<SeedDemoDataCommand>
{
    public SeedDemoDataValidator()
    {
        RuleFor(x => x).Custom((_, context) =>
        {
            // Guard: SeedDemoData yalnızca geliştirme/test ortamında çalışmalı.
            // Ortam kontrolü handler seviyesinde yapılır; validator pipeline'ı boş geçmez.
        });
    }
}
