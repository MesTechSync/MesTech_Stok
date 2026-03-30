using MediatR;

namespace MesTech.Application.Features.Cargo.Queries.GetCargoProviders;

public record GetCargoProvidersQuery(Guid TenantId) : IRequest<IReadOnlyList<CargoProviderDto>>;

public sealed class CargoProviderDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ContractInfo { get; set; }
    public int AvgDeliveryDays { get; set; }
}
