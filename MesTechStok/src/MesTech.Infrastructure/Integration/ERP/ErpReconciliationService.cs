using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// Reconciliation service — compares MesTech data with ERP data.
/// Produces a diff report. No automatic corrections — manual approval required.
/// </summary>
public sealed class ErpReconciliationService
{
    private readonly IErpAdapterFactory _adapterFactory;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ErpReconciliationService> _logger;

    public ErpReconciliationService(
        IErpAdapterFactory adapterFactory,
        IProductRepository productRepository,
        ILogger<ErpReconciliationService> logger)
    {
        _adapterFactory = adapterFactory;
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <summary>
    /// Reconciles stock quantities between MesTech and ERP.
    /// </summary>
    public async Task<ReconciliationReport> ReconcileStockAsync(
        ErpProvider provider, CancellationToken ct = default)
    {
        var report = new ReconciliationReport
        {
            Provider = provider,
            ReconciliationType = "Stock",
            GeneratedAt = DateTimeOffset.UtcNow
        };

        var adapter = _adapterFactory.GetAdapter(provider);
        if (adapter is not IErpStockCapable stockCapable)
        {
            _logger.LogWarning("[Reconciliation] Adapter {Provider} does not support IErpStockCapable", provider);
            return report;
        }

        // 1. Get MesTech stock levels from DB
        var mestechProducts = await _productRepository.GetAllAsync().ConfigureAwait(false);
        report.TotalMesTechRecords = mestechProducts.Count;

        // 2. Get ERP stock levels via adapter
        var erpStockLevels = await stockCapable.GetStockLevelsAsync(ct).ConfigureAwait(false);
        report.TotalErpRecords = erpStockLevels.Count;

        // 3. Build ERP lookup by product code
        var erpLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var level in erpStockLevels)
        {
            if (!string.IsNullOrEmpty(level.ProductCode))
                erpLookup[level.ProductCode] = level.Quantity;
        }

        // 4. Compare
        foreach (var product in mestechProducts)
        {
            var code = product.SKU ?? product.Barcode ?? string.Empty;
            if (string.IsNullOrEmpty(code)) continue;

            if (erpLookup.TryGetValue(code, out var erpQty))
            {
                var mestechQty = product.Stock;
                if (mestechQty != erpQty)
                {
                    report.Diffs.Add(new ReconciliationDiff
                    {
                        Code = code,
                        Name = product.Name ?? code,
                        MesTechValue = mestechQty.ToString(),
                        ErpValue = erpQty.ToString(),
                        Difference = erpQty - mestechQty,
                        SuggestedAction = "ERP'den güncelle"
                    });
                    report.DiffCount++;
                }
                else
                {
                    report.MatchedCount++;
                }
                erpLookup.Remove(code);
            }
            else
            {
                report.MissingInErpCount++;
            }
        }

        // Remaining in erpLookup = missing in MesTech
        report.MissingInMesTechCount = erpLookup.Count;

        _logger.LogInformation(
            "[Reconciliation] {Provider} stock: {Matched} matched, {Diff} diffs, {MissingErp} missing in ERP, {MissingMT} missing in MesTech",
            provider, report.MatchedCount, report.DiffCount, report.MissingInErpCount, report.MissingInMesTechCount);

        return report;
    }
}

/// <summary>Reconciliation result report.</summary>
public sealed class ReconciliationReport
{
    public ErpProvider Provider { get; set; }
    public string ReconciliationType { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public int TotalMesTechRecords { get; set; }
    public int TotalErpRecords { get; set; }
    public int MatchedCount { get; set; }
    public int DiffCount { get; set; }
    public int MissingInErpCount { get; set; }
    public int MissingInMesTechCount { get; set; }
    public List<ReconciliationDiff> Diffs { get; set; } = new();
}

/// <summary>Single reconciliation difference item.</summary>
public sealed class ReconciliationDiff
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MesTechValue { get; set; } = string.Empty;
    public string ErpValue { get; set; } = string.Empty;
    public int Difference { get; set; }
    public string SuggestedAction { get; set; } = string.Empty;
}
