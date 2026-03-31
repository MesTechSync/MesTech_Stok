using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;

public sealed class DeleteErpAccountMappingHandler : IRequestHandler<DeleteErpAccountMappingCommand, bool>
{
    private readonly IErpAccountMappingRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteErpAccountMappingHandler> _logger;

    public DeleteErpAccountMappingHandler(
        IErpAccountMappingRepository repo,
        IUnitOfWork unitOfWork,
        ILogger<DeleteErpAccountMappingHandler> logger)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteErpAccountMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = await _repo.GetByIdAsync(request.MappingId, cancellationToken).ConfigureAwait(false);
        if (mapping is null || mapping.TenantId != request.TenantId)
            return false;

        _repo.Remove(mapping);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("ErpAccountMapping deleted: {MesTechCode} ↔ {ErpCode}",
            mapping.MesTechAccountCode, mapping.ErpAccountCode);

        return true;
    }
}
