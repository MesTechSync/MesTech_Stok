using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetCategoriesPaged;

public sealed class GetCategoriesPagedHandler : IRequestHandler<GetCategoriesPagedQuery, PagedCategoryResult>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesPagedHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public async Task<PagedCategoryResult> Handle(GetCategoriesPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allCategories = await _categoryRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var filtered = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? allCategories
            : allCategories.Where(c =>
                c.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalCount = filtered.Count;
        var items = filtered
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
            .ToList();

        return new PagedCategoryResult
        {
            Items = items,
            TotalCount = totalCount,
        };
    }
}
