using MesTech.Application.Interfaces;
using MesTech.Application.Services;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Formatters.Dropshipping;
using MesTech.Infrastructure.Integration.Accounting;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Integration.Auth;
using MesTech.Infrastructure.Integration.Dropshipping;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Infrastructure.Integration.Invoice.Config;
using MesTech.Infrastructure.Integration.Orchestration;
using MesTech.Infrastructure.Integration.Soap;
using MesTech.Infrastructure.Integration.FeedParsers;
using MesTech.Infrastructure.Integration.Scraping;
using MesTech.Infrastructure.Integration.Security;
using MesTech.Infrastructure.Integration.Webhooks;
using MesTech.Infrastructure.Services;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Middleware;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.DependencyInjection;

public static class IntegrationServiceRegistration
{
    public static IServiceCollection AddIntegrationServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Adapters — singleton with manually created HttpClient
        services.AddSingleton<TrendyolAdapter>(sp =>
            new TrendyolAdapter(new HttpClient(), sp.GetRequiredService<ILogger<TrendyolAdapter>>()));
        services.AddSingleton<OpenCartAdapter>(sp =>
            new OpenCartAdapter(new HttpClient(), sp.GetRequiredService<ILogger<OpenCartAdapter>>()));

        // Multi-registration: each adapter also registered as IIntegratorAdapter
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<TrendyolAdapter>());
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<OpenCartAdapter>());

        // Dalga 3: Ciceksepeti + Hepsiburada marketplace adapters
        services.AddSingleton<CiceksepetiAdapter>(sp =>
            new CiceksepetiAdapter(new HttpClient(), sp.GetRequiredService<ILogger<CiceksepetiAdapter>>()));
        services.AddSingleton<HepsiburadaAdapter>(sp =>
            new HepsiburadaAdapter(new HttpClient(), sp.GetRequiredService<ILogger<HepsiburadaAdapter>>()));

        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<CiceksepetiAdapter>());
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<HepsiburadaAdapter>());

        // Dalga 4: Pazarama marketplace adapter — OAuth2, async batch, 2-stage cargo
        services.AddSingleton<PazaramaAdapter>(sp =>
            new PazaramaAdapter(new HttpClient(), sp.GetRequiredService<ILogger<PazaramaAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<PazaramaAdapter>());

        // Dalga 6: Amazon TR (SP-API) — LWA OAuth2, catalog, orders, feeds
        services.AddSingleton<AmazonTrAdapter>(sp =>
            new AmazonTrAdapter(
                new HttpClient(),
                sp.GetRequiredService<ILogger<AmazonTrAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<AmazonTrAdapter>());

        // Dalga 7: Bitrix24 CRM adapter — OAuth2, deal/contact sync, batch API
        services.AddSingleton<Bitrix24Adapter>(sp =>
            new Bitrix24Adapter(new HttpClient(), sp.GetRequiredService<ILogger<Bitrix24Adapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<Bitrix24Adapter>());
        services.AddSingleton<IBitrix24Adapter>(sp => sp.GetRequiredService<Bitrix24Adapter>());

        // Dalga 5: N11 SOAP adapter — Singleton (IAdapterFactory is singleton)
        services.AddSingleton<N11Adapter>();
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<N11Adapter>());

        // Dalga 8: eBay — OAuth2 Client Credentials (foundation, full impl TODO H28)
        services.AddSingleton<EbayAdapter>(sp =>
            new EbayAdapter(new HttpClient(), sp.GetRequiredService<ILogger<EbayAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<EbayAdapter>());

        // Dalga 8: Ozon — Client-Id + Api-Key header auth (foundation, full impl TODO H28)
        services.AddSingleton<OzonAdapter>(sp =>
            new OzonAdapter(new HttpClient(), sp.GetRequiredService<ILogger<OzonAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<OzonAdapter>());

        // Dalga 8: PTT AVM — username/password Bearer token (foundation, full impl TODO H28)
        services.AddSingleton<PttAvmAdapter>(sp =>
            new PttAvmAdapter(new HttpClient(), sp.GetRequiredService<ILogger<PttAvmAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<PttAvmAdapter>());

        // Dalga 3: Cargo adapters — SCOPED (multi-tenant credential isolation)
        services.AddScoped<ICargoAdapter>(sp =>
            new YurticiKargoAdapter(new HttpClient(), sp.GetRequiredService<ILogger<YurticiKargoAdapter>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new ArasKargoAdapter(new HttpClient(), sp.GetRequiredService<ILogger<ArasKargoAdapter>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new SuratKargoAdapter(new HttpClient(), sp.GetRequiredService<ILogger<SuratKargoAdapter>>()));

        // Factory — receives IEnumerable<IIntegratorAdapter>
        services.AddSingleton<IAdapterFactory, AdapterFactory>();

        // Cargo factory + selector + auto-shipment — Scoped (depends on scoped cargo adapters)
        services.AddScoped<ICargoProviderFactory, CargoProviderFactory>();
        services.AddScoped<ICargoProviderSelector, CargoProviderSelector>();
        services.AddScoped<IAutoShipmentService, AutoShipmentService>();

        // Orchestrator — receives IAdapterFactory
        services.AddSingleton<IIntegratorOrchestrator, IntegratorOrchestratorService>();

        // Webhook receiver
        services.AddScoped<IWebhookReceiverService, WebhookReceiverService>();

        // Token cache (in-memory, Redis swap later)
        services.AddSingleton<ITokenCacheProvider, InMemoryTokenCacheProvider>();

        // Invoice providers — factory pattern (replaces feature flag)
        services.AddScoped<MockInvoiceProvider>();
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<MockInvoiceProvider>());
        services.AddScoped<SovosInvoiceProvider>(sp =>
            new SovosInvoiceProvider(new HttpClient(), sp.GetRequiredService<ILogger<SovosInvoiceProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<SovosInvoiceProvider>());
        services.AddScoped<ParasutInvoiceProvider>(sp =>
            new ParasutInvoiceProvider(new HttpClient(), sp.GetRequiredService<ILogger<ParasutInvoiceProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<ParasutInvoiceProvider>());

        // Dalga 5 DEV3 delivery: TrendyolEFaturam + ELogo providers
        services.AddScoped<TrendyolEFaturamProvider>(sp =>
            new TrendyolEFaturamProvider(new HttpClient(), sp.GetRequiredService<ILogger<TrendyolEFaturamProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<TrendyolEFaturamProvider>());

        services.AddScoped<ELogoInvoiceProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var soapClient = new SimpleSoapClient(new HttpClient(), loggerFactory.CreateLogger<SimpleSoapClient>());
            return new ELogoInvoiceProvider(new HttpClient(), soapClient, sp.GetRequiredService<ILogger<ELogoInvoiceProvider>>());
        });
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<ELogoInvoiceProvider>());

        // Dalga 5 DEV3 delivery: BirFatura, DijitalPlanet, GibPortal, HBFatura providers
        services.AddScoped<BirFaturaProvider>(sp =>
            new BirFaturaProvider(new HttpClient(), sp.GetRequiredService<ILogger<BirFaturaProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<BirFaturaProvider>());

        services.AddScoped<DijitalPlanetProvider>(sp =>
            new DijitalPlanetProvider(new HttpClient(), sp.GetRequiredService<ILogger<DijitalPlanetProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<DijitalPlanetProvider>());

        services.AddScoped<GibPortalProvider>(sp =>
            new GibPortalProvider(new HttpClient(), sp.GetRequiredService<ILogger<GibPortalProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<GibPortalProvider>());

        services.AddScoped<HBFaturaProvider>(sp =>
            new HBFaturaProvider(new HttpClient(), sp.GetRequiredService<ILogger<HBFaturaProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<HBFaturaProvider>());

        services.AddScoped<IInvoiceProviderFactory, InvoiceProviderFactory>();

        // Dalga 5: Invoice Adapters — wrap existing providers via composition
        services.AddScoped<MockInvoiceAdapter>(sp =>
            new MockInvoiceAdapter(sp.GetRequiredService<MockInvoiceProvider>()));
        services.AddScoped<IInvoiceAdapter>(sp => sp.GetRequiredService<MockInvoiceAdapter>());

        services.AddScoped<SovosInvoiceAdapter>(sp =>
            new SovosInvoiceAdapter(
                sp.GetRequiredService<SovosInvoiceProvider>(),
                sp.GetRequiredService<IGibMukellefService>(),
                sp.GetRequiredService<ILogger<SovosInvoiceAdapter>>()));
        services.AddScoped<IInvoiceAdapter>(sp => sp.GetRequiredService<SovosInvoiceAdapter>());

        services.AddScoped<ParasutInvoiceAdapter>(sp =>
            new ParasutInvoiceAdapter(
                sp.GetRequiredService<ParasutInvoiceProvider>(),
                sp.GetRequiredService<ILogger<ParasutInvoiceAdapter>>()));
        services.AddScoped<IInvoiceAdapter>(sp => sp.GetRequiredService<ParasutInvoiceAdapter>());

        services.AddScoped<TrendyolEFaturamAdapter>(sp =>
            new TrendyolEFaturamAdapter(
                sp.GetRequiredService<TrendyolEFaturamProvider>(),
                sp.GetRequiredService<ILogger<TrendyolEFaturamAdapter>>()));
        services.AddScoped<IInvoiceAdapter>(sp => sp.GetRequiredService<TrendyolEFaturamAdapter>());

        services.AddScoped<IInvoiceAdapterFactory, InvoiceAdapterFactory>();

        // Dalga 5 A-04: GIB mukellef sorgu servisi — cached VKN lookup
        services.AddMemoryCache();
        services.AddScoped<IGibMukellefService, GibMukellefService>();

        // Product scraper service — URL-based product info via platform APIs
        services.AddScoped<IProductScraperService>(sp =>
            new ProductScraperService(new HttpClient(), sp.GetRequiredService<ILogger<ProductScraperService>>()));

        // G10 A-08: Paraşüt accounting integration
        services.AddScoped<IParasutAccountingService>(sp =>
            new ParasutAccountingService(
                new HttpClient { BaseAddress = new Uri("https://api.parasut.com/v4/") },
                sp.GetRequiredService<IIncomeRepository>(),
                sp.GetRequiredService<IExpenseRepository>(),
                sp.GetRequiredService<ILogger<ParasutAccountingService>>()));

        // Dalga 7.5: Feed parsers — keyed by FeedFormat for SupplierFeedSyncJob resolution
        services.AddKeyedScoped<IFeedParserService, XmlFeedParser>(FeedFormat.Xml);
        services.AddKeyedScoped<IFeedParserService, CsvFeedParser>(FeedFormat.Csv);
        services.AddKeyedScoped<IFeedParserService, ExcelFeedParser>(FeedFormat.Excel);
        services.AddKeyedScoped<IFeedParserService, JsonFeedParser>(FeedFormat.Json);

        // Also register as IEnumerable<IFeedParserService> for FeedHealthCheckService
        services.AddScoped<IFeedParserService, XmlFeedParser>();
        services.AddScoped<IFeedParserService, CsvFeedParser>();
        services.AddScoped<IFeedParserService, ExcelFeedParser>();
        services.AddScoped<IFeedParserService, JsonFeedParser>();

        // Dalga 7.5: Feed health check service
        services.AddScoped<FeedHealthCheckService>(sp =>
            new FeedHealthCheckService(
                new HttpClient(),
                sp.GetServices<IFeedParserService>(),
                sp.GetRequiredService<ILogger<FeedHealthCheckService>>()));

        // ENT-DROP-SENTEZ-001 Sprint A: CSV export + encoding detector
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<IEncodingDetectorService, EncodingDetectorService>();

        // ENT-DROP-IMP-SPRINT-B — DEV 3 Görev A: Dropshipping export formatters (6 platform)
        services.AddScoped<IDropshippingExportFormatter, XmlDropshippingFormatter>();
        services.AddScoped<IDropshippingExportFormatter, CsvDropshippingFormatter>();
        services.AddScoped<IDropshippingExportFormatter, ExcelDropshippingFormatter>();
        services.AddScoped<IDropshippingExportFormatter, TrendyolDropshippingFormatter>();
        services.AddScoped<IDropshippingExportFormatter, HepsisellerDropshippingFormatter>();
        services.AddScoped<IDropshippingExportFormatter, N11DropshippingFormatter>();

        // ENT-DROP-IMP-SPRINT-D — DEV 3 Task D-08: Ozon formatter (9/9)
        services.AddScoped<IDropshippingExportFormatter, OzonDropshippingFormatter>();

        // ENT-DROP-IMP-SPRINT-B — DEV 3 Görev B: FeedReliabilityScoreService (saf hesaplama)
        services.AddScoped<FeedReliabilityScoreService>();

        // ENT-DROP-IMP-SPRINT-D — DEV 3 Task D-06: Image downloader (Polly + SHA256 dedup)
        services.AddHttpClient("ImageDownloader", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "MesTech/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IImageDownloadService, ImageDownloadService>();

        // ENT-DROP-IMP-SPRINT-D — DEV 3 Task D-07: Feed credential şifreleme (AES-256-GCM)
        // Encryption key: "FeedCredentials:EncryptionKey" config değerinden okunur.
        // Yoksa ephemeral key (development only — her restart'ta yeni key).
        services.AddSingleton<IFeedCredentialProtector>(sp =>
        {
            var key = configuration?["FeedCredentials:EncryptionKey"];
            return new FeedCredentialProtector(key);
        });

        // New invoice provider configs — Dalga 5 (D-06): adapters to be built by DEV3
        if (configuration is not null)
        {
            services.Configure<ELogoInvoiceConfig>(opt =>
                configuration.GetSection(ELogoInvoiceConfig.Section).Bind(opt));
            services.Configure<BirFaturaInvoiceConfig>(opt =>
                configuration.GetSection(BirFaturaInvoiceConfig.Section).Bind(opt));
            services.Configure<DijiitalPlanetInvoiceConfig>(opt =>
                configuration.GetSection(DijiitalPlanetInvoiceConfig.Section).Bind(opt));
            services.Configure<GibPortalInvoiceConfig>(opt =>
                configuration.GetSection(GibPortalInvoiceConfig.Section).Bind(opt));
            services.Configure<HBFaturaInvoiceConfig>(opt =>
                configuration.GetSection(HBFaturaInvoiceConfig.Section).Bind(opt));

            // API Key middleware options — Dalga 5 (IP-6): protects MesTech Web API (port 5100)
            services.AddApiKeyAuthentication(configuration);
        }

        // ENT-DROP-IMP-SPRINT-D — DEV 1 Task D-02: Category fuzzy mapper (Levenshtein + keyword overlap)
        services.AddScoped<ICategoryMapperService, CategoryAutoMapper>();

        // ENT-DROP-IMP-SPRINT-D — DEV 1 Task D-01: IServiceLocatorBridge (sadece App.xaml.cs için)
        services.AddSingleton<IServiceLocatorBridge, ServiceLocatorBridge>();

        return services;
    }
}
