using MediatR;
using MesTech.Application.Commands.CreateCategory;

namespace MesTech.Application.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest<CategoryCommandResult>;
