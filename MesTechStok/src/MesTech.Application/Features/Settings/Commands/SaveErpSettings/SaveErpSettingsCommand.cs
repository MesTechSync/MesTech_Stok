using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Settings.Commands.SaveErpSettings;

public record SaveErpSettingsCommand(
    Guid TenantId,
    ErpProvider ErpProvider,
    bool AutoSyncStock,
    bool AutoSyncInvoice,
    int StockSyncPeriodMinutes,
    int PriceSyncPeriodMinutes
) : IRequest<SaveErpSettingsResult>;
