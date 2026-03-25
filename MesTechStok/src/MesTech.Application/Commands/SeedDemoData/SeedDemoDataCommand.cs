using MediatR;

namespace MesTech.Application.Commands.SeedDemoData;

public record SeedDemoDataCommand() : IRequest<SeedDemoDataResult>;

public sealed class SeedDemoDataResult
{
    public bool IsSuccess { get; set; }
    public bool WasSkipped { get; set; }
    public string? Message { get; set; }
}
