using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetCategories;

public sealed class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryListDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryListDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var categories = request.ActiveOnly
            ? await _categoryRepository.GetActiveAsync(cancellationToken)
            : await _categoryRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return categories.Select(c => new CategoryListDto
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryId,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive,
            Color = c.Color,
            Icon = c.Icon,
        }).ToList();
    }
}
