using FluentValidation;

namespace MesTech.Application.Features.Shipping.Queries.DownloadShipmentLabel;

public sealed class DownloadShipmentLabelValidator : AbstractValidator<DownloadShipmentLabelQuery>
{
    private static readonly string[] AllowedFormats = ["PDF", "ZPL", "PNG"];

    public DownloadShipmentLabelValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f.ToUpperInvariant()))
            .WithMessage("Format PDF, ZPL veya PNG olmali.");
    }
}
