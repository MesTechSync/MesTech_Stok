using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public sealed class DeleteFeedSourceValidator : AbstractValidator<DeleteFeedSourceCommand>
{
    public DeleteFeedSourceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
