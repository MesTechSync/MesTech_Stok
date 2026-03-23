using MediatR;
using MesTech.Domain.Entities;

namespace MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;

public record GetIncomeExpenseListQuery(
    Guid TenantId,
    string? Type = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<IncomeExpenseListResultDto>;

public record IncomeExpenseListResultDto(
    IReadOnlyList<IncomeExpenseItemDto> Items,
    int TotalCount);

public record IncomeExpenseItemDto(
    Guid Id, string Description, decimal Amount,
    string Type, string Source, DateTime Date, Guid? OrderId);
