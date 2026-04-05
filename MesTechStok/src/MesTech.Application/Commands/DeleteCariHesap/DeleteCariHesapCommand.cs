using MediatR;

namespace MesTech.Application.Commands.DeleteCariHesap;

public record DeleteCariHesapCommand(Guid Id) : IRequest<DeleteCariHesapResult>;

public sealed class DeleteCariHesapResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
