using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;

public sealed class CreateDropshipSupplierValidator : AbstractValidator<CreateDropshipSupplierCommand>
{
    public CreateDropshipSupplierValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.WebsiteUrl).MaximumLength(500).When(x => x.WebsiteUrl != null);
        RuleFor(x => x.MarkupType).IsInEnum();
    }
}
