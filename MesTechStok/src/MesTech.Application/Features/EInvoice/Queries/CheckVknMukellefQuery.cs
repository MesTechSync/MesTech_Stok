using MesTech.Application.Interfaces;
using MediatR;

namespace MesTech.Application.Features.EInvoice.Queries;

public record CheckVknMukellefQuery(string Vkn) : IRequest<VknMukellefResult>;
