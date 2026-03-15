using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Queries.GetSidebarCategories;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// H32: Migrated from Core.AppDbContext to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class GetSidebarCategoriesHandler : IRequestHandler<GetSidebarCategoriesQuery, IReadOnlyList<SidebarCategoryDto>>
{
    private readonly IServiceProvider _serviceProvider;

    public GetSidebarCategoriesHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<IReadOnlyList<SidebarCategoryDto>> Handle(GetSidebarCategoriesQuery request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InfraDbContext>();

        var query = db.Categories.AsNoTracking().Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            query = query.Where(c => c.Name.Contains(request.SearchText));
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new SidebarCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
            })
            .ToListAsync(cancellationToken);
    }
}
