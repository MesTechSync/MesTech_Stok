using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Reports.ErpReconciliationReport;

public sealed class ErpReconciliationReportHandler
    : IRequestHandler<ErpReconciliationReportQuery, ErpReconciliationReportDto>
{
    private readonly IErpAdapterFactory _erpFactory;
    private readonly ICounterpartyRepository _counterpartyRepo;
    private readonly ILogger<ErpReconciliationReportHandler> _logger;

    public ErpReconciliationReportHandler(
        IErpAdapterFactory erpFactory,
        ICounterpartyRepository counterpartyRepo,
        ILogger<ErpReconciliationReportHandler> logger)
    {
        _erpFactory = erpFactory;
        _counterpartyRepo = counterpartyRepo;
        _logger = logger;
    }

    public async Task<ErpReconciliationReportDto> Handle(
        ErpReconciliationReportQuery request, CancellationToken cancellationToken)
    {
        // MesTech cari hesapları
        var mesTechContacts = await _counterpartyRepo.GetAllAsync(
            request.TenantId, null, null, cancellationToken);

        // ERP cari hesapları — adapter resolution can throw various exceptions (config, DI, etc.)
#pragma warning disable CA1031 // Intentional broad catch — report returns partial data on any adapter failure
        IErpAdapter adapter;
        try
        {
            adapter = _erpFactory.GetAdapter(request.ErpProvider);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ERP adapter not found for provider {Provider}", request.ErpProvider);
            return new ErpReconciliationReportDto
            {
                ErpProvider = request.ErpProvider,
                TotalMesTechContacts = mesTechContacts.Count,
                TotalErpContacts = 0,
                MatchedCount = 0,
                UnmatchedInMesTech = mesTechContacts.Count,
                UnmatchedInErp = 0,
                UnmatchedItems = mesTechContacts
                    .Select(c => new UnmatchedContactDto("MesTech", c.Name, c.VKN, "ERP adapter bulunamadı"))
                    .ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
#pragma warning restore CA1031

        // ERP query can fail due to network, auth, or data issues — report degrades gracefully
#pragma warning disable CA1031 // Intentional broad catch — report returns empty ERP data on query failure
        DTOs.ERP.ErpAccountDto[] erpAccounts;
        try
        {
            var accounts = await adapter.GetAccountBalancesAsync(cancellationToken);
            erpAccounts = accounts.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ERP accounts query failed for {Provider}", request.ErpProvider);
            erpAccounts = [];
        }
#pragma warning restore CA1031

        // İsim bazlı eşleştirme (ErpAccountDto'da VKN yok, AccountName ile eşleştir)
        var mesTechByName = mesTechContacts
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var erpByName = erpAccounts
            .Where(e => !string.IsNullOrWhiteSpace(e.AccountName))
            .GroupBy(e => e.AccountName.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        int matched = 0;
        var unmatched = new List<UnmatchedContactDto>();

        // MesTech'te var, ERP'de yok
        foreach (var kvp in mesTechByName)
        {
            if (erpByName.ContainsKey(kvp.Key))
                matched++;
            else
                unmatched.Add(new UnmatchedContactDto("MesTech", kvp.Value.Name, kvp.Value.VKN, "ERP'de yok"));
        }

        // ERP'de var, MesTech'te yok
        foreach (var kvp in erpByName)
        {
            if (!mesTechByName.ContainsKey(kvp.Key))
                unmatched.Add(new UnmatchedContactDto("ERP", kvp.Value.AccountName, kvp.Value.AccountCode, "MesTech'te yok"));
        }

        return new ErpReconciliationReportDto
        {
            ErpProvider = request.ErpProvider,
            TotalMesTechContacts = mesTechContacts.Count,
            TotalErpContacts = erpAccounts.Length,
            MatchedCount = matched,
            UnmatchedInMesTech = mesTechContacts.Count - matched,
            UnmatchedInErp = erpAccounts.Length - matched,
            UnmatchedItems = unmatched,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
