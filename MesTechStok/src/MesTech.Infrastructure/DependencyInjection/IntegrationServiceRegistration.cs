using Hangfire;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Application.Services;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Formatters.Dropshipping;
using MesTech.Infrastructure.Integration.Accounting;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Integration.Auth;
using Microsoft.Extensions.Options;
using MesTech.Infrastructure.Integration.Dropshipping;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Infrastructure.Integration.Feed;
using MesTech.Infrastructure.Integration.Fulfillment;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Infrastructure.Integration.Invoice.Config;
using MesTech.Infrastructure.Integration.Jobs;
using MesTech.Infrastructure.Integration.Orchestration;
using MesTech.Infrastructure.Integration.Payment;
using MesTech.Infrastructure.Integration.ERP;
using MesTech.Infrastructure.Integration.ERP.BizimHesap;
using MesTech.Infrastructure.Integration.ERP.Logo;
using MesTech.Infrastructure.Integration.ERP.Netsis;
using MesTech.Infrastructure.Integration.ERP.Nebim;
using ERPNext = MesTech.Infrastructure.Integration.ERP.ERPNext;
using MesTech.Infrastructure.Integration.ERP.Parasut;
using MesTech.Infrastructure.Integration.Settlement;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
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
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace MesTech.Infrastructure.DependencyInjection;

public static class IntegrationServiceRegistration
{
    public static IServiceCollection AddIntegrationServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Register named HttpClients — replaces all new HttpClient() calls
        services.AddIntegrationHttpClients();

        // Dalga 14 S3: Trendyol options — sandbox toggle + environment-aware URLs
        if (configuration is not null)
            services.Configure<TrendyolOptions>(configuration.GetSection(TrendyolOptions.Section));

        // Adapters — singleton with IHttpClientFactory-managed HttpClient
        services.AddSingleton<TrendyolAdapter>(sp =>
            new TrendyolAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Trendyol),
                sp.GetRequiredService<ILogger<TrendyolAdapter>>(),
                sp.GetService<IOptions<TrendyolOptions>>()));
        services.AddSingleton<OpenCartAdapter>(sp =>
            new OpenCartAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.OpenCart),
                sp.GetRequiredService<ILogger<OpenCartAdapter>>()));

        // Multi-registration: each adapter also registered as IIntegratorAdapter
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<TrendyolAdapter>());
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<OpenCartAdapter>());

        // Dalga 3: Ciceksepeti + Hepsiburada marketplace adapters
        services.AddSingleton<CiceksepetiAdapter>(sp =>
            new CiceksepetiAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Ciceksepeti),
                sp.GetRequiredService<ILogger<CiceksepetiAdapter>>()));

        // K1c-02: HepsiburadaTokenService — OAuth token management (55-min cache, auto-refresh)
        services.AddSingleton<HepsiburadaTokenService>(sp =>
            new HepsiburadaTokenService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.HepsiburadaToken),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<HepsiburadaTokenService>>()));

        // K1c-03: HepsiburadaAdapter now receives HepsiburadaTokenService for OAuth token auth
        services.AddSingleton<HepsiburadaAdapter>(sp =>
            new HepsiburadaAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Hepsiburada),
                sp.GetRequiredService<ILogger<HepsiburadaAdapter>>(),
                sp.GetRequiredService<HepsiburadaTokenService>()));

        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<CiceksepetiAdapter>());
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<HepsiburadaAdapter>());

        // Dalga 4: Pazarama marketplace adapter — OAuth2, async batch, 2-stage cargo
        services.AddSingleton<PazaramaAdapter>(sp =>
            new PazaramaAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Pazarama),
                sp.GetRequiredService<ILogger<PazaramaAdapter>>(),
                sp.GetRequiredService<IHttpClientFactory>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<PazaramaAdapter>());

        // Dalga 6: Amazon TR (SP-API) — LWA OAuth2, catalog, orders, feeds
        services.AddSingleton<AmazonTrAdapter>(sp =>
            new AmazonTrAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.AmazonTr),
                sp.GetRequiredService<ILogger<AmazonTrAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<AmazonTrAdapter>());

        // Dalga 11: Amazon EU (SP-API) — 7 EU marketplaces (DE, FR, IT, ES, NL, SE, PL)
        services.AddSingleton<AmazonEuAdapter>(sp =>
            new AmazonEuAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.AmazonEu),
                sp.GetRequiredService<ILogger<AmazonEuAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<AmazonEuAdapter>());

        // Dalga 7: Bitrix24 CRM adapter — OAuth2, deal/contact sync, batch API
        services.AddSingleton<Bitrix24Adapter>(sp =>
            new Bitrix24Adapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Bitrix24),
                sp.GetRequiredService<ILogger<Bitrix24Adapter>>(),
                sp.GetRequiredService<IHttpClientFactory>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<Bitrix24Adapter>());
        services.AddSingleton<IBitrix24Adapter>(sp => sp.GetRequiredService<Bitrix24Adapter>());

        // Dalga 5: N11 SOAP adapter — Singleton (IAdapterFactory is singleton)
        services.AddSingleton<N11Adapter>();
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<N11Adapter>());

        // Dalga 8: eBay — OAuth2 Client Credentials
        if (configuration is not null)
            services.Configure<EbayOptions>(configuration.GetSection(EbayOptions.Section));
        services.AddSingleton<EbayAdapter>(sp =>
            new EbayAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Ebay),
                sp.GetRequiredService<ILogger<EbayAdapter>>(),
                sp.GetService<IOptions<EbayOptions>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<EbayAdapter>());

        // Dalga 8: Ozon — Client-Id + Api-Key header auth
        services.AddSingleton<OzonAdapter>(sp =>
            new OzonAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Ozon),
                sp.GetRequiredService<ILogger<OzonAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<OzonAdapter>());

        // Dalga 8: PTT AVM — username/password Bearer token
        services.AddSingleton<PttAvmAdapter>(sp =>
            new PttAvmAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.PttAvm),
                sp.GetRequiredService<ILogger<PttAvmAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<PttAvmAdapter>());

        // Dalga 14 S3: YurticiKargo options — sandbox toggle + environment-aware URLs
        if (configuration is not null)
            services.Configure<YurticiKargoOptions>(configuration.GetSection(YurticiKargoOptions.Section));

        // Dalga 3: Cargo adapters — SCOPED (multi-tenant credential isolation)
        services.AddScoped<ICargoAdapter>(sp =>
            new YurticiKargoAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.YurticiKargo),
                sp.GetRequiredService<ILogger<YurticiKargoAdapter>>(),
                sp.GetService<IOptions<YurticiKargoOptions>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new ArasKargoAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.ArasKargo),
                sp.GetRequiredService<ILogger<ArasKargoAdapter>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new SuratKargoAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.SuratKargo),
                sp.GetRequiredService<ILogger<SuratKargoAdapter>>()));

        // Phase B: +4 kargo adaptor (MNG, PTT, HepsiJet, Sendeo)
        services.AddScoped<ICargoAdapter>(sp =>
            new MngKargoAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.MngKargo),
                sp.GetRequiredService<ILogger<MngKargoAdapter>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new PttKargoAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.PttKargo),
                sp.GetRequiredService<ILogger<PttKargoAdapter>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new HepsiJetCargoAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.HepsiJet),
                sp.GetRequiredService<ILogger<HepsiJetCargoAdapter>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new SendeoCargoAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Sendeo),
                sp.GetRequiredService<ILogger<SendeoCargoAdapter>>()));

        // Decorator: wrap all IIntegratorAdapter registrations with Prometheus instrumentation
        services.DecorateAllIntegratorAdapters();

        // Factory — receives IEnumerable<IIntegratorAdapter> (now instrumented)
        services.AddSingleton<IAdapterFactory, AdapterFactory>();

        // Adapter health check — parallel PingAsync for all adapters
        services.AddSingleton<Integration.Health.AdapterHealthService>();
        services.AddSingleton<Integration.Health.PlatformHealthHistory>();
        services.AddSingleton<Application.Interfaces.IPlatformHealthProvider, Integration.Health.PlatformHealthProvider>();

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

        // Dalga 9: UBL-TR 1.2 XML builder + validator
        services.AddSingleton<IUblTrXmlBuilder, UblTrXmlBuilder>();
        services.AddSingleton<IUblTrXmlValidator, UblTrXmlValidator>();

        services.AddScoped<SovosInvoiceProvider>(sp =>
            new SovosInvoiceProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Sovos),
                sp.GetRequiredService<ILogger<SovosInvoiceProvider>>(),
                sp.GetRequiredService<IUblTrXmlBuilder>(),
                sp.GetRequiredService<IUblTrXmlValidator>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<SovosInvoiceProvider>());
        services.AddScoped<IEInvoiceProvider>(sp => sp.GetRequiredService<SovosInvoiceProvider>());
        services.AddScoped<ParasutInvoiceProvider>(sp =>
            new ParasutInvoiceProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.ParasutInvoice),
                sp.GetRequiredService<ILogger<ParasutInvoiceProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<ParasutInvoiceProvider>());

        // Dalga 5 DEV3 delivery: TrendyolEFaturam + ELogo providers
        services.AddScoped<TrendyolEFaturamProvider>(sp =>
            new TrendyolEFaturamProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.TrendyolEFaturam),
                sp.GetRequiredService<ILogger<TrendyolEFaturamProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<TrendyolEFaturamProvider>());

        services.AddScoped<ELogoInvoiceProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var soapClient = new SimpleSoapClient(
                factory.CreateClient(IntegrationHttpClientRegistry.ClientNames.ELogoSoap),
                loggerFactory.CreateLogger<SimpleSoapClient>());
            return new ELogoInvoiceProvider(
                factory.CreateClient(IntegrationHttpClientRegistry.ClientNames.ELogo),
                soapClient,
                sp.GetRequiredService<ILogger<ELogoInvoiceProvider>>());
        });
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<ELogoInvoiceProvider>());

        // Dalga 5 DEV3 delivery: BirFatura, DijitalPlanet, GibPortal, HBFatura providers
        services.AddScoped<BirFaturaProvider>(sp =>
            new BirFaturaProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.BirFatura),
                sp.GetRequiredService<ILogger<BirFaturaProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<BirFaturaProvider>());

        services.AddScoped<DijitalPlanetProvider>(sp =>
            new DijitalPlanetProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.DijitalPlanet),
                sp.GetRequiredService<ILogger<DijitalPlanetProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<DijitalPlanetProvider>());

        services.AddScoped<GibPortalProvider>(sp =>
            new GibPortalProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.GibPortal),
                sp.GetRequiredService<ILogger<GibPortalProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<GibPortalProvider>());

        services.AddScoped<HBFaturaProvider>(sp =>
            new HBFaturaProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.HBFatura),
                sp.GetRequiredService<ILogger<HBFaturaProvider>>()));
        services.AddScoped<IInvoiceProvider>(sp => sp.GetRequiredService<HBFaturaProvider>());

        services.AddScoped<IInvoiceProviderFactory, InvoiceProviderFactory>();

        // Dalga 10 E-10: GibPortalEInvoiceProvider — REST token-based e-Arsiv Portal (IEInvoiceProvider)
        if (configuration is not null)
            services.Configure<GibPortalEInvoiceOptions>(configuration.GetSection(GibPortalEInvoiceOptions.Section));
        services.AddScoped<GibPortalEInvoiceProvider>(sp =>
            new GibPortalEInvoiceProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.GibPortalEInvoice),
                sp.GetRequiredService<ILogger<GibPortalEInvoiceProvider>>(),
                sp.GetService<IOptions<GibPortalEInvoiceOptions>>()));
        if (configuration?.GetValue<bool>("Invoice:GibPortalEInvoice:Enabled") == true)
            services.AddScoped<IEInvoiceProvider>(sp => sp.GetRequiredService<GibPortalEInvoiceProvider>());

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
            new ProductScraperService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.ProductScraper),
                sp.GetRequiredService<ILogger<ProductScraperService>>()));

        // G10 A-08: Paraşüt accounting integration
        services.AddScoped<IParasutAccountingService>(sp =>
            new ParasutAccountingService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.ParasutAccounting),
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

        // Panel-E: DEV-E2 — FeedParserFactory replaces IServiceProvider in SupplierFeedSyncJob
        services.AddScoped<IFeedParserFactory, FeedParserFactory>();

        // Dalga 7.5: Feed health check service
        services.AddScoped<FeedHealthCheckService>(sp =>
            new FeedHealthCheckService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.FeedHealthCheck),
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
        // Desktop App.xaml.cs hala kullanıyor — Desktop DI migration sonrası kaldırılacak
        services.AddSingleton<IServiceLocatorBridge, ServiceLocatorBridge>();

        // MUH-01: Settlement parsers — platform-specific settlement data parsers (Singleton — stateless)
        services.AddSingleton<ISettlementParser, TrendyolSettlementParser>();
        services.AddSingleton<ISettlementParser, AmazonSettlementParser>();
        services.AddSingleton<ISettlementParser, HepsiburadaSettlementParser>();

        // MUH-02: 5 new settlement parsers (Ciceksepeti, N11, Pazarama, OpenCart, eBay)
        services.AddSingleton<ISettlementParser, CiceksepetiSettlementParser>();
        services.AddSingleton<ISettlementParser, N11SettlementParser>();
        services.AddSingleton<ISettlementParser, PazaramaSettlementParser>();
        services.AddSingleton<ISettlementParser, OpenCartSettlementParser>();
        services.AddSingleton<ISettlementParser, EbaySettlementParser>();

        // Settlement parser factory — auto-discovers all registered ISettlementParser (8 total)
        services.AddSingleton<ISettlementParserFactory, SettlementParserFactory>();

        // Dalga 14 S3: Parasut options — sandbox toggle + environment-aware URLs
        if (configuration is not null)
            services.Configure<ParasutOptions>(configuration.GetSection(ParasutOptions.Section));

        // İ-14 R-01: Register Polly-resilient named HttpClients for all ERP adapters
        services.AddErpResilientHttpClients();

        // Invoice provider Polly resilience — retry + circuit breaker on all invoice HttpClients
        services.AddInvoiceResilientHttpClients();

        // MUH-02: Parasut ERP adapter — OAuth2 CC token service + JSON:API sync
        services.AddSingleton<ParasutTokenService>(sp =>
            new ParasutTokenService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ErpResiliencePolicies.ClientNames.ParasutToken),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<ParasutTokenService>>(),
                sp.GetService<IOptions<ParasutOptions>>()));
        services.AddScoped<ParasutERPAdapter>(sp =>
            new ParasutERPAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ErpResiliencePolicies.ClientNames.Parasut),
                sp.GetRequiredService<ParasutTokenService>(),
                sp.GetRequiredService<IOrderRepository>(),
                sp.GetRequiredService<IInvoiceRepository>(),
                sp.GetRequiredService<ILogger<ParasutERPAdapter>>(),
                sp.GetService<IOptions<ParasutOptions>>()));
        services.AddScoped<IERPAdapter>(sp => sp.GetRequiredService<ParasutERPAdapter>());
        services.AddScoped<IErpAdapter>(sp => sp.GetRequiredService<ParasutERPAdapter>());

        // MUH-03 + Dalga 12: Logo ERP adapter — L-Object REST API Bearer token + JSON sync
        // Implements both IERPAdapter (legacy batch) and IErpAdapter (Dalga 11 ID-based)
        services.AddSingleton<LogoTokenService>(sp =>
            new LogoTokenService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ErpResiliencePolicies.ClientNames.LogoToken),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<LogoTokenService>>()));
        services.AddScoped<LogoERPAdapter>(sp =>
            new LogoERPAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ErpResiliencePolicies.ClientNames.Logo),
                sp.GetRequiredService<LogoTokenService>(),
                sp.GetRequiredService<IOrderRepository>(),
                sp.GetRequiredService<IInvoiceRepository>(),
                sp.GetRequiredService<ILogger<LogoERPAdapter>>()));
        services.AddScoped<IERPAdapter>(sp => sp.GetRequiredService<LogoERPAdapter>());
        services.AddScoped<IErpAdapter>(sp => sp.GetRequiredService<LogoERPAdapter>());

        // MUH-03: BizimHesap ERP adapter — API Key auth + REST JSON sync
        services.AddScoped<BizimHesapApiClient>();
        services.AddScoped<BizimHesapERPAdapter>(sp =>
            new BizimHesapERPAdapter(
                sp.GetRequiredService<BizimHesapApiClient>(),
                sp.GetRequiredService<IOrderRepository>(),
                sp.GetRequiredService<IInvoiceRepository>(),
                sp.GetRequiredService<ILogger<BizimHesapERPAdapter>>()));
        services.AddScoped<IERPAdapter>(sp => sp.GetRequiredService<BizimHesapERPAdapter>());
        services.AddScoped<IErpAdapter>(sp => sp.GetRequiredService<BizimHesapERPAdapter>());

        // Dalga 13: Netsis ERP adapter — Basic Auth REST API + JSON sync
        services.AddScoped<NetsisERPAdapter>(sp =>
            new NetsisERPAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ErpResiliencePolicies.ClientNames.Netsis),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IOrderRepository>(),
                sp.GetRequiredService<ILogger<NetsisERPAdapter>>()));
        services.AddScoped<IErpAdapter>(sp => sp.GetRequiredService<NetsisERPAdapter>());

        // Dalga 15: Nebim V3 ERP adapter — API Key auth + REST JSON sync
        if (configuration is not null)
            services.Configure<NebimOptions>(configuration.GetSection(NebimOptions.SectionName));
        services.AddScoped<NebimERPAdapter>(sp =>
            new NebimERPAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ErpResiliencePolicies.ClientNames.Nebim),
                sp.GetRequiredService<IOptions<NebimOptions>>(),
                sp.GetRequiredService<IOrderRepository>(),
                sp.GetRequiredService<ILogger<NebimERPAdapter>>()));
        services.AddScoped<IErpAdapter>(sp => sp.GetRequiredService<NebimERPAdapter>());

        // G128: ERPNext REST adapter — Frappe API, token auth
        if (configuration is not null)
            services.Configure<ERPNext.ERPNextOptions>(configuration.GetSection(ERPNext.ERPNextOptions.Section));
        services.AddScoped<ERPNext.ERPNextRestAdapter>(sp =>
            new ERPNext.ERPNextRestAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ErpResiliencePolicies.ClientNames.ERPNext),
                sp.GetRequiredService<ILogger<ERPNext.ERPNextRestAdapter>>(),
                sp.GetRequiredService<IOptions<ERPNext.ERPNextOptions>>()));
        services.AddScoped<IERPAdapter>(sp => sp.GetRequiredService<ERPNext.ERPNextRestAdapter>());
        services.AddScoped<Application.Interfaces.IErpBridgeService>(sp => sp.GetRequiredService<ERPNext.ERPNextRestAdapter>());

        // MUH-02 + Dalga 12: ERP adapter factory — Scoped (depends on scoped IERPAdapter + IErpAdapter instances)
        // Implements both IERPAdapterFactory (legacy) and IErpAdapterFactory (Dalga 11)
        services.AddScoped<ERPAdapterFactory>();
        services.AddScoped<IERPAdapterFactory>(sp => sp.GetRequiredService<ERPAdapterFactory>());
        services.AddScoped<IErpAdapterFactory>(sp => sp.GetRequiredService<ERPAdapterFactory>());

        // İ-14 D-03: ERP reconciliation service — compares MesTech vs ERP stock data
        services.AddScoped<ErpReconciliationService>();

        // İ-14 D-03: ERP conflict resolver — deterministic merge strategies for ERP sync conflicts
        services.AddSingleton<ErpConflictResolver>();

        // MUH-02: Canonical finance mapper — normalizes entities for ERP sync
        services.AddSingleton<CanonicalFinanceMapper>();

        // -----------------------------------------------------------------------
        // DALGA 9 — On Muhasebe & Kargo Genisletme (completed)
        // -----------------------------------------------------------------------
        // Phase B DONE: +4 kargo adaptor (MNG, PTT, HepsiJet, Sendeo) registered above.
        // -----------------------------------------------------------------------

        // -----------------------------------------------------------------------
        // DALGA 10 — Sosyal Ticaret Feed Adapter'lari + PayTR Odeme
        // -----------------------------------------------------------------------

        // Social feed adapters — Scoped (multi-tenant credential isolation)
        services.AddScoped<GoogleMerchantFeedAdapter>();
        services.AddScoped<ISocialFeedAdapter>(sp => sp.GetRequiredService<GoogleMerchantFeedAdapter>());

        services.AddScoped<FacebookShopFeedAdapter>();
        services.AddScoped<ISocialFeedAdapter>(sp => sp.GetRequiredService<FacebookShopFeedAdapter>());

        services.AddScoped<InstagramShopFeedAdapter>();
        services.AddScoped<ISocialFeedAdapter>(sp => sp.GetRequiredService<InstagramShopFeedAdapter>());

        // PayTR payment adapters — Scoped (config may vary per tenant in future)
        services.AddScoped<PayTRDirectAdapter>(sp =>
            new PayTRDirectAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.PayTRDirect),
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                sp.GetRequiredService<ILogger<PayTRDirectAdapter>>()));
        services.AddScoped<IPaymentProvider>(sp => sp.GetRequiredService<PayTRDirectAdapter>());

        services.AddScoped<PayTRiFrameAdapter>(sp =>
            new PayTRiFrameAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.PayTRiFrame),
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                sp.GetRequiredService<ILogger<PayTRiFrameAdapter>>()));
        services.AddScoped<IPaymentProvider>(sp => sp.GetRequiredService<PayTRiFrameAdapter>());

        // Payment gateways — IPaymentGateway (iyzico, Stripe)
        if (configuration is not null)
        {
            services.Configure<IyzicoOptions>(configuration.GetSection("Iyzico"));
            services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
        }
        services.AddScoped<IyzicoPaymentGateway>();
        services.AddScoped<StripePaymentGateway>();
        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<IyzicoPaymentGateway>());
        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<StripePaymentGateway>());
        services.AddSingleton<Application.Interfaces.IPaymentWebhookSecretProvider, Services.PaymentWebhookSecretProvider>();

        // Dalga 10 C-01: Shopify — X-Shopify-Access-Token, cursor pagination, HMAC-SHA256 webhooks
        if (configuration is not null)
            services.Configure<ShopifyOptions>(configuration.GetSection(ShopifyOptions.Section));
        services.AddSingleton<ShopifyAdapter>(sp =>
            new ShopifyAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Shopify),
                sp.GetRequiredService<ILogger<ShopifyAdapter>>(),
                sp.GetService<IOptions<ShopifyOptions>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<ShopifyAdapter>());

        // Dalga 10 C-02: WooCommerce — Basic Auth (ConsumerKey:ConsumerSecret), page-based pagination
        if (configuration is not null)
            services.Configure<WooCommerceOptions>(configuration.GetSection(WooCommerceOptions.Section));
        services.AddSingleton<WooCommerceAdapter>(sp =>
            new WooCommerceAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.WooCommerce),
                sp.GetRequiredService<ILogger<WooCommerceAdapter>>(),
                sp.GetService<IOptions<WooCommerceOptions>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<WooCommerceAdapter>());

        // Dalga 10 C-03 / Panel E: Etsy — full OAuth2 PKCE, Listings + Receipts + Inventory API
        if (configuration is not null)
            services.Configure<EtsyOptions>(configuration.GetSection(EtsyOptions.Section));
        services.AddSingleton<EtsyAdapter>(sp =>
            new EtsyAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Etsy),
                sp.GetRequiredService<ILogger<EtsyAdapter>>(),
                sp.GetService<IOptions<EtsyOptions>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<EtsyAdapter>());

        // Dalga 10 C-03 / D11-09: Zalando — OAuth2 Client Credentials, page-based pagination, EUR currency
        if (configuration is not null)
            services.Configure<ZalandoOptions>(configuration.GetSection(ZalandoOptions.Section));
        services.AddSingleton<ZalandoAdapter>(sp =>
            new ZalandoAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Zalando),
                sp.GetRequiredService<ILogger<ZalandoAdapter>>(),
                sp.GetService<IOptions<ZalandoOptions>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<ZalandoAdapter>());

        // SocialFeedRefreshJob — Scoped (depends on AppDbContext + feed adapters)
        services.AddScoped<SocialFeedRefreshJob>();

        // -----------------------------------------------------------------------
        // DALGA 11 — Fulfillment Centers + ERP Event-Driven Sync
        // -----------------------------------------------------------------------

        // B1: AmazonFBAAdapter — SP-API Inbound + Inventory (Scoped: credentials may vary per tenant)
        // NOTE: Credentials are injected from IConfiguration / user-secrets at runtime.
        // Registration uses a factory lambda to allow configuration binding.
        services.AddScoped<AmazonFBAAdapter>(sp =>
        {
            var cfg = sp.GetService<IConfiguration>();
            var refreshToken = cfg?["Amazon:FBA:RefreshToken"] ?? string.Empty;
            var clientId = cfg?["Amazon:FBA:ClientId"] ?? string.Empty;
            var clientSecret = cfg?["Amazon:FBA:ClientSecret"] ?? string.Empty;
            var sellerId = cfg?["Amazon:FBA:SellerId"] ?? string.Empty;
            return new AmazonFBAAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.AmazonFBA),
                sp.GetRequiredService<ILogger<AmazonFBAAdapter>>(),
                refreshToken,
                clientId,
                clientSecret,
                sellerId,
                sp.GetRequiredService<IHttpClientFactory>());
        });
        services.AddScoped<IFulfillmentProvider>(sp => sp.GetRequiredService<AmazonFBAAdapter>());

        // B2: HepsilojistikAdapter — Basic Auth (Scoped: credentials may vary per tenant)
        services.AddScoped<HepsilojistikAdapter>(sp =>
        {
            var cfg = sp.GetService<IConfiguration>();
            var merchantId = cfg?["Hepsilojistik:MerchantId"] ?? string.Empty;
            var apiKey = cfg?["Hepsilojistik:ApiKey"] ?? string.Empty;
            return new HepsilojistikAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(IntegrationHttpClientRegistry.ClientNames.Hepsilojistik),
                sp.GetRequiredService<ILogger<HepsilojistikAdapter>>(),
                merchantId,
                apiKey);
        });
        services.AddScoped<IFulfillmentProvider>(sp => sp.GetRequiredService<HepsilojistikAdapter>());

        // B4: FulfillmentProviderFactory — Scoped (depends on scoped IFulfillmentProvider instances)
        services.AddScoped<IFulfillmentProviderFactory, FulfillmentProviderFactory>();

        // B3: ERPSyncHandler — Scoped + registered as IERPSyncHandler
        services.AddScoped<ERPSyncHandler>();
        services.AddScoped<IERPSyncHandler>(sp => sp.GetRequiredService<ERPSyncHandler>());

        // B5: FulfillmentStockSyncJob — Scoped (depends on IFulfillmentProviderFactory + AppDbContext)
        services.AddScoped<FulfillmentStockSyncJob>();

        // -----------------------------------------------------------------------

        return services;
    }
}
