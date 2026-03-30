using FluentValidation;

namespace MesTech.Application.Queries.GetBarcodeScanLogs;

public sealed class GetBarcodeScanLogsValidator : AbstractValidator<GetBarcodeScanLogsQuery>
{
    public GetBarcodeScanLogsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.BarcodeFilter).MaximumLength(200).When(x => x.BarcodeFilter != null);
        RuleFor(x => x.SourceFilter).MaximumLength(200).When(x => x.SourceFilter != null);
    }
}
