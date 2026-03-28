using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Returns.Queries.GetReturnList;

public sealed class GetReturnListHandler : IRequestHandler<GetReturnListQuery, IReadOnlyList<ReturnListItemDto>>
{
    private readonly IReturnRequestRepository _returnRepo;

    public GetReturnListHandler(IReturnRequestRepository returnRepo) => _returnRepo = returnRepo;

    public async Task<IReadOnlyList<ReturnListItemDto>> Handle(GetReturnListQuery request, CancellationToken cancellationToken)
    {
        var returns = await _returnRepo.GetByTenantAsync(request.TenantId, request.Count, cancellationToken);

        return returns.Select(r => new ReturnListItemDto
        {
            Id = r.Id,
            OrderId = r.OrderId,
            Status = r.Status.ToString(),
            Reason = r.Reason.ToString(),
            RefundAmount = r.RefundAmount,
            LineCount = r.Lines.Count,
            CreatedAt = r.CreatedAt
        }).ToList().AsReadOnly();
    }
}
