using MediatR;

namespace MesTech.Application.Queries.GetSidebarCategories;

public record GetSidebarCategoriesQuery(string? SearchText = null) : IRequest<IReadOnlyList<SidebarCategoryDto>>;

public sealed class SidebarCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
