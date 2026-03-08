using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Queries.GetDashboardData;
using MesTechStok.Desktop.Data;

namespace MesTechStok.Desktop.Handlers;

public class GetActiveCategoriesCountHandler : IRequestHandler<GetActiveCategoriesCountQuery, int>
{
    private readonly IServiceProvider _serviceProvider;

    public GetActiveCategoriesCountHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<int> Handle(GetActiveCategoriesCountQuery request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DesktopDbContext>();
        var count = context.Categories.Count(c => c.IsActive);
        return Task.FromResult(count);
    }
}
