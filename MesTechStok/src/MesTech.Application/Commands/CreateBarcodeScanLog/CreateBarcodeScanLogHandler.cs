using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateBarcodeScanLog;

public sealed class CreateBarcodeScanLogHandler
    : IRequestHandler<CreateBarcodeScanLogCommand, CreateBarcodeScanLogResult>
{
    private readonly IBarcodeScanLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBarcodeScanLogHandler(
        IBarcodeScanLogRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreateBarcodeScanLogResult> Handle(
        CreateBarcodeScanLogCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var log = new BarcodeScanLog
        {
            Barcode = request.Barcode,
            Format = request.Format,
            Source = request.Source,
            DeviceId = request.DeviceId,
            IsValid = request.IsValid,
            ValidationMessage = request.ValidationMessage,
            RawLength = request.RawLength,
            TimestampUtc = DateTime.UtcNow,
            CorrelationId = request.CorrelationId
        };

        await _repository.AddAsync(log, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateBarcodeScanLogResult
        {
            IsSuccess = true,
            LogId = log.Id
        };
    }
}
