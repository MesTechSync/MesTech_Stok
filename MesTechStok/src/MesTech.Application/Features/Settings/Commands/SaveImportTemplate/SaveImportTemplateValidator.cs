using FluentValidation;

namespace MesTech.Application.Features.Settings.Commands.SaveImportTemplate;

public sealed class SaveImportTemplateValidator : AbstractValidator<SaveImportTemplateCommand>
{
    public SaveImportTemplateValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TemplateName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FileFormat).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ColumnMappings).NotNull();
    }
}
