using MediatR;

namespace MesTech.Application.Queries.GetCategories;

public record GetCategoriesQuery(bool ActiveOnly = true) : IRequest<IReadOnlyList<CategoryListDto>>;

public sealed class CategoryListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
}
