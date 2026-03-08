using MediatR;
using MesTech.Application.Commands.CreateCategory;

namespace MesTech.Application.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Code,
    bool IsActive
) : IRequest<CategoryCommandResult>;
