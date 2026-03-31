using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;

public sealed class CreateErpAccountMappingHandler
    : IRequestHandler<CreateErpAccountMappingCommand, CreateErpAccountMappingResult>
{
    private readonly IErpAccountMappingRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateErpAccountMappingHandler> _logger;

    public CreateErpAccountMappingHandler(
        IErpAccountMappingRepository repo,
        IUnitOfWork unitOfWork,
        ILogger<CreateErpAccountMappingHandler> logger)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateErpAccountMappingResult> Handle(
        CreateErpAccountMappingCommand request, CancellationToken cancellationToken)
    {
        // Duplicate check — ayni MesTech veya ERP kodu zaten eslesmis mi?
        var existingByMesTech = await _repo.FindByMesTechCodeAsync(request.TenantId, request.MesTechCode, cancellationToken).ConfigureAwait(false);
        if (existingByMesTech is not null)
            return CreateErpAccountMappingResult.Failure($"MesTech hesabi '{request.MesTechCode}' zaten eslesmis.");

        var existingByErp = await _repo.FindByErpCodeAsync(request.TenantId, request.ErpCode, cancellationToken).ConfigureAwait(false);
        if (existingByErp is not null)
            return CreateErpAccountMappingResult.Failure($"ERP hesabi '{request.ErpCode}' zaten eslesmis.");

        var mapping = ErpAccountMapping.Create(
            request.TenantId, ErpProvider.Parasut,
            request.MesTechCode, request.MesTechName, request.MesTechType,
            request.ErpCode, request.ErpName);

        await _repo.AddAsync(mapping, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("ErpAccountMapping created: {MesTechCode} ↔ {ErpCode}", request.MesTechCode, request.ErpCode);

        return CreateErpAccountMappingResult.Success(mapping.Id);
    }
}
