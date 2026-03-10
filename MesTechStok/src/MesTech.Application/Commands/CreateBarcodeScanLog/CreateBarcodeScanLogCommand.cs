using MediatR;

namespace MesTech.Application.Commands.CreateBarcodeScanLog;

public record CreateBarcodeScanLogCommand(
    string Barcode,
    string Format,
    string Source,
    string? DeviceId = null,
    bool IsValid = true,
    string? ValidationMessage = null,
    int RawLength = 0,
    string? CorrelationId = null
) : IRequest<CreateBarcodeScanLogResult>;

public class CreateBarcodeScanLogResult
{
    public bool IsSuccess { get; set; }
    public Guid LogId { get; set; }
    public string? ErrorMessage { get; set; }
}
