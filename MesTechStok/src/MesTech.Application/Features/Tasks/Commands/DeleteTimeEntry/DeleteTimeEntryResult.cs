namespace MesTech.Application.Features.Tasks.Commands.DeleteTimeEntry;

public sealed record DeleteTimeEntryResult(bool IsSuccess, string? ErrorMessage = null);
