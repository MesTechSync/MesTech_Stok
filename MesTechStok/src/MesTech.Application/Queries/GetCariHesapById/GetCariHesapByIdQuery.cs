using MesTech.Application.DTOs.Accounting;
using MediatR;

namespace MesTech.Application.Queries.GetCariHesapById;

public record GetCariHesapByIdQuery(Guid Id) : IRequest<CariHesapDto?>;
