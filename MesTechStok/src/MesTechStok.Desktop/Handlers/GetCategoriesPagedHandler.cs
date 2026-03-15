using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Queries.GetCategoriesPaged;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// H32: Migrated from Core.AppDbContext to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class GetCategoriesPagedHandler : IRequestHandler<GetCategoriesPagedQuery, PagedCategoryResult>
{
    private readonly IServiceProvider _serviceProvider;

    public GetCategoriesPagedHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<PagedCategoryResult> Handle(GetCategoriesPagedQuery request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InfraDbContext>();

        var query = db.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Code.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedAt,
                ModifiedDate = c.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedCategoryResult
        {
            Items = items,
            TotalCount = totalCount,
        };
    }
}
