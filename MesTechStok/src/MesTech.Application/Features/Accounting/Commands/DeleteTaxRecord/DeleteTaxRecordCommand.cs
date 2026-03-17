using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.DeleteTaxRecord;

public record DeleteTaxRecordCommand(Guid Id) : IRequest;
