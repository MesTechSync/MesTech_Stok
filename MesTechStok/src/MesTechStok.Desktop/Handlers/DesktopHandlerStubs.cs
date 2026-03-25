using MediatR;

namespace MesTechStok.Desktop.Handlers;

public record GetCategoriesPagedQuery(int Page, int PageSize) : IRequest<GetCategoriesPagedResult>;

public class GetCategoriesPagedResult
{
    public System.Collections.Generic.List<string> Categories { get; set; } = new();
    public int TotalCount { get; set; }
}

public class GetCategoriesPagedHandler : IRequestHandler<GetCategoriesPagedQuery, GetCategoriesPagedResult>
{
    public Task<GetCategoriesPagedResult> Handle(GetCategoriesPagedQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetCategoriesPagedResult());
    }
}
