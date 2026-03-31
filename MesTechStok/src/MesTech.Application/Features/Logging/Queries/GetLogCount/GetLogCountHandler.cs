using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Logging.Queries.GetLogCount;

public sealed class GetLogCountHandler : IRequestHandler<GetLogCountQuery, long>
{
    private readonly ILogEntryRepository _repo;

    public GetLogCountHandler(ILogEntryRepository repo)
    {
        _repo = repo;
    }

    public async Task<long> Handle(GetLogCountQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _repo.GetCountAsync(request.TenantId, request.Category, cancellationToken).ConfigureAwait(false);
    }
}
