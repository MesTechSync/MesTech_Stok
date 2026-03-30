using FluentValidation;

namespace MesTech.Application.Features.System.Users;

public sealed class GetUsersValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersValidator() { }
}
