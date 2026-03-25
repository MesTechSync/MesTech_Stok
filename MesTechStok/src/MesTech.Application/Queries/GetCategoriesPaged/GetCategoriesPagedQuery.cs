using MediatR;

namespace MesTech.Application.Queries.GetCategoriesPaged;

public record GetCategoriesPagedQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedCategoryResult>;

public sealed class PagedCategoryResult
{
    public IReadOnlyList<CategoryItemDto> Items { get; set; } = Array.Empty<CategoryItemDto>();
    public int TotalCount { get; set; }
}

public sealed class CategoryItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
