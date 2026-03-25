using FluentValidation;

namespace MesTech.Application.Commands.DeleteCategory;

public sealed class DeleteCategoryValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
