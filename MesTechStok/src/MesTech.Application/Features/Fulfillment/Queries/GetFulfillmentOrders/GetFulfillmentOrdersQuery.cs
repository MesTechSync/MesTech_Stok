using MediatR;
using MesTech.Application.DTOs.Fulfillment;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;

/// <summary>
/// Belirtilen tarihten itibaren fulfillment merkezinden gonderilen siparisleri sorgular.
/// </summary>
public record GetFulfillmentOrdersQuery(
    FulfillmentCenter Center,
    DateTime Since
) : IRequest<IReadOnlyList<FulfillmentOrderResult>>;
