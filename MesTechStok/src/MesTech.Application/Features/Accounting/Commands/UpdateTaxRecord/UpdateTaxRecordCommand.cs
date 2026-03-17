using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;

public record UpdateTaxRecordCommand(
    Guid Id,
    bool MarkAsPaid = false
) : IRequest;
