using MediatR;

namespace MesTech.Application.Features.Settings.Queries.GetFulfillmentSettings;

public record GetFulfillmentSettingsQuery(Guid TenantId) : IRequest<FulfillmentSettingsDto>;

public sealed class FulfillmentSettingsDto
{
    public FulfillmentProviderDto? AmazonFba { get; set; }
    public FulfillmentProviderDto? Hepsilojistik { get; set; }
}

public sealed class FulfillmentProviderDto
{
    public bool IsConfigured { get; set; }
    public bool AutoReplenish { get; set; }
    public string ConnectionStatus { get; set; } = "Baglanti yok";
    public DateTime? LastSyncAt { get; set; }
}
