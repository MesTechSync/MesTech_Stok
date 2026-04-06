using FluentValidation;

namespace MesTech.Application.Features.Platform.Commands.DeleteStore;

public sealed class DeleteStoreValidator : AbstractValidator<DeleteStoreCommand>
{
    public DeleteStoreValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
