using MediatR;

namespace MesTech.Application.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string Code,
    bool IsActive = true
) : IRequest<CategoryCommandResult>;

public sealed class CategoryCommandResult
{
    public bool IsSuccess { get; set; }
    public Guid CategoryId { get; set; }
    public string? ErrorMessage { get; set; }
}
