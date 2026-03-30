using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetSalaryRecordById;

public sealed class GetSalaryRecordByIdValidator : AbstractValidator<GetSalaryRecordByIdQuery>
{
    public GetSalaryRecordByIdValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
