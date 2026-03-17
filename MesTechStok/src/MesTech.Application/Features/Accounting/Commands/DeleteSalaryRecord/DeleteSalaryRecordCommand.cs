using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;

public record DeleteSalaryRecordCommand(Guid Id) : IRequest;
