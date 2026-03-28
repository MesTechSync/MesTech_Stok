using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Accounting.Commands.ParseAndImportSettlement;

/// <summary>
/// Raw settlement data → ISettlementParserFactory → SettlementBatch → DB.
/// Fixes S3 KOPUK: 8 registered parsers now have a handler that uses them.
/// </summary>
public sealed class ParseAndImportSettlementHandler : IRequestHandler<ParseAndImportSettlementCommand, Guid>
{
    private readonly ISettlementParserFactory _parserFactory;
    private readonly ISettlementBatchRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ParseAndImportSettlementHandler> _logger;

    public ParseAndImportSettlementHandler(
        ISettlementParserFactory parserFactory,
        ISettlementBatchRepository repository,
        IUnitOfWork uow,
        ILogger<ParseAndImportSettlementHandler> logger)
    {
        _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> Handle(ParseAndImportSettlementCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "ParseAndImportSettlement: Platform={Platform}, TenantId={TenantId}, Format={Format}, Size={Size}",
            request.Platform, request.TenantId, request.Format, request.RawData.Length);

        var parser = _parserFactory.GetParser(request.Platform);

        using var stream = new MemoryStream(request.RawData);
        var batch = await parser.ParseAsync(request.TenantId, stream, request.Format, cancellationToken)
            .ConfigureAwait(false);

        var lines = await parser.ParseLinesAsync(batch, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "ParseAndImportSettlement: Parsed {LineCount} lines, Gross={Gross}, Net={Net}",
            lines.Count, batch.TotalGross, batch.TotalNet);

        await _repository.AddAsync(batch, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return batch.Id;
    }
}
