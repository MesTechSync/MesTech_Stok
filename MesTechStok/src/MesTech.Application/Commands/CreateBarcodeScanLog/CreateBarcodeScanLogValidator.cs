using FluentValidation;

namespace MesTech.Application.Commands.CreateBarcodeScanLog;

public class CreateBarcodeScanLogValidator : AbstractValidator<CreateBarcodeScanLogCommand>
{
    public CreateBarcodeScanLogValidator()
    {
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Format).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Source).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DeviceId).MaximumLength(500).When(x => x.DeviceId != null);
        RuleFor(x => x.ValidationMessage).MaximumLength(500).When(x => x.ValidationMessage != null);
        RuleFor(x => x.CorrelationId).MaximumLength(500).When(x => x.CorrelationId != null);
    }
}
