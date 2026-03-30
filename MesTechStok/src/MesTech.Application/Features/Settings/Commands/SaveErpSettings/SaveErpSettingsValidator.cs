using FluentValidation;

namespace MesTech.Application.Features.Settings.Commands.SaveErpSettings;

public sealed class SaveErpSettingsValidator : AbstractValidator<SaveErpSettingsCommand>
{
    public SaveErpSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ErpProvider).IsInEnum();
        RuleFor(x => x.StockSyncPeriodMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.PriceSyncPeriodMinutes).InclusiveBetween(1, 1440);
    }
}
