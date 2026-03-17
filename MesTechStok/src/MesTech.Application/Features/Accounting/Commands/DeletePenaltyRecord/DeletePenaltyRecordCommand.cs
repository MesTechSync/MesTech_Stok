using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;

public record DeletePenaltyRecordCommand(Guid Id) : IRequest;
