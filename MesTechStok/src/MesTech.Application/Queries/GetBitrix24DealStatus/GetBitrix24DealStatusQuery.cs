using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetBitrix24DealStatus;

public record GetBitrix24DealStatusQuery(Guid OrderId) : IRequest<Bitrix24DealStatusDto?>;
