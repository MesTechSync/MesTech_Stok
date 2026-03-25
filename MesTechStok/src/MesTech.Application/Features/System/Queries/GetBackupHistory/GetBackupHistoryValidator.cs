using FluentValidation;

namespace MesTech.Application.Features.System.Queries.GetBackupHistory;

public sealed class GetBackupHistoryValidator : AbstractValidator<GetBackupHistoryQuery>
{
    public GetBackupHistoryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
