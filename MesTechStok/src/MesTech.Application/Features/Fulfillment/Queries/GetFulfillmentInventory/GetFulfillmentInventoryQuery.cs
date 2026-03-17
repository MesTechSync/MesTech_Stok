using MediatR;
using MesTech.Application.DTOs.Fulfillment;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;

/// <summary>
/// Belirtilen fulfillment merkezindeki SKU'lar icin envanter durumunu sorgular.
/// </summary>
public record GetFulfillmentInventoryQuery(
    FulfillmentCenter Center,
    IReadOnlyList<string> Skus
) : IRequest<FulfillmentInventory>;
