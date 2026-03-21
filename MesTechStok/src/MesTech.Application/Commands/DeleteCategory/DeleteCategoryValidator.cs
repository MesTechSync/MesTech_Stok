using FluentValidation;

namespace MesTech.Application.Commands.DeleteCategory;

public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
