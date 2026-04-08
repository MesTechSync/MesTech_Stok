namespace MesTech.Application.Features.Tasks.Commands.DeleteProject;

public sealed record DeleteProjectResult(bool IsSuccess, string? ErrorMessage = null);
