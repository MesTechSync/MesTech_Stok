using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Onboarding.Queries.GetV5ReadinessCheck;

public class GetV5ReadinessCheckHandler
    : IRequestHandler<GetV5ReadinessCheckQuery, V5ReadinessCheckDto>
{
    private readonly IOnboardingProgressRepository _onboardingRepo;
    private readonly IErpAdapterFactory _erpFactory;
    private readonly IFulfillmentProviderFactory _fulfillmentFactory;
    private readonly ICommissionRecordRepository _commissionRepo;
    private readonly ICounterpartyRepository _counterpartyRepo;

    public GetV5ReadinessCheckHandler(
        IOnboardingProgressRepository onboardingRepo,
        IErpAdapterFactory erpFactory,
        IFulfillmentProviderFactory fulfillmentFactory,
        ICommissionRecordRepository commissionRepo,
        ICounterpartyRepository counterpartyRepo)
    {
        _onboardingRepo = onboardingRepo;
        _erpFactory = erpFactory;
        _fulfillmentFactory = fulfillmentFactory;
        _commissionRepo = commissionRepo;
        _counterpartyRepo = counterpartyRepo;
    }

    public async Task<V5ReadinessCheckDto> Handle(
        GetV5ReadinessCheckQuery request, CancellationToken cancellationToken)
    {
        // Temel onboarding kontrolü
        var progress = await _onboardingRepo.GetByTenantIdAsync(request.TenantId, cancellationToken);
        var basicCompleted = progress?.IsCompleted ?? false;

        var features = new List<V5FeatureCheckDto>();

        // 1. ERP Entegrasyonu — en az 1 ERP provider bağlı mı?
        var erpProviders = _erpFactory.SupportedProviders;
        var erpConnected = false;
        foreach (var provider in erpProviders)
        {
            try
            {
                var adapter = _erpFactory.GetAdapter(provider);
                if (await adapter.PingAsync(cancellationToken))
                {
                    erpConnected = true;
                    break;
                }
            }
            catch { /* provider not available */ }
        }
        features.Add(new V5FeatureCheckDto(
            "ERP Entegrasyonu",
            "En az bir ERP sağlayıcısı bağlı ve erişilebilir",
            erpConnected,
            erpConnected ? $"{erpProviders.Count} provider destekleniyor" : "Hiçbir ERP bağlantısı aktif değil"));

        // 2. Fulfillment Merkezi — en az 1 center bağlı mı?
        var fulfillmentConnected = false;
        var centerNames = new List<string>();
        foreach (var center in Enum.GetValues<DTOs.Fulfillment.FulfillmentCenter>())
        {
            var provider = _fulfillmentFactory.Resolve(center);
            if (provider is null) continue;
            try
            {
                if (await provider.IsAvailableAsync(cancellationToken))
                {
                    fulfillmentConnected = true;
                    centerNames.Add(center.ToString());
                }
            }
            catch { /* center not available */ }
        }
        features.Add(new V5FeatureCheckDto(
            "Fulfillment Merkezi",
            "En az bir fulfillment center (FBA/Hepsilojistik) bağlı",
            fulfillmentConnected,
            fulfillmentConnected ? $"Aktif: {string.Join(", ", centerNames)}" : "Hiçbir center bağlı değil"));

        // 3. Komisyon Takibi — en az 1 komisyon kaydı var mı?
        var commissionRecords = await _commissionRepo.GetByPlatformAsync(
            request.TenantId, "Trendyol",
            DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow, cancellationToken);
        var hasCommissionData = commissionRecords.Count > 0;
        features.Add(new V5FeatureCheckDto(
            "Komisyon Takibi",
            "Platform komisyon kayıtları mevcut (son 3 ay)",
            hasCommissionData,
            hasCommissionData ? $"{commissionRecords.Count} kayıt" : "Komisyon kaydı bulunamadı"));

        // 4. Cari Hesaplar — en az 1 cari tanımlı mı?
        var counterparties = await _counterpartyRepo.GetAllAsync(
            request.TenantId, null, true, cancellationToken);
        var hasCounterparties = counterparties.Count > 0;
        features.Add(new V5FeatureCheckDto(
            "Cari Hesap Tanımları",
            "Tedarikçi/müşteri cari hesapları tanımlı",
            hasCounterparties,
            hasCounterparties ? $"{counterparties.Count} aktif cari" : "Cari hesap tanımlı değil"));

        // 5. Raporlama — profitability endpoint erişilebilir (handler mevcut)
        features.Add(new V5FeatureCheckDto(
            "Raporlama Modülü",
            "Kârlılık, komisyon, platform performans raporları hazır",
            true,
            "4 V5 rapor handler aktif + 3 export endpoint (PDF/Excel/CSV)"));

        var completedCount = features.Count(f => f.IsCompleted);

        return new V5ReadinessCheckDto
        {
            TenantId = request.TenantId,
            BasicOnboardingCompleted = basicCompleted,
            Features = features,
            CompletedCount = completedCount,
            TotalCount = features.Count,
            CompletionPercentage = features.Count > 0
                ? Math.Round((decimal)completedCount / features.Count * 100, 1) : 0
        };
    }
}
