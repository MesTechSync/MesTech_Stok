using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.DependencyInjection;

/// <summary>
/// Named HttpClient registrations for all integration adapters.
/// Replaces direct <c>new HttpClient()</c> calls with IHttpClientFactory-managed instances.
/// Socket exhaustion prevention + DNS rotation + configurable timeouts.
/// </summary>
public static class IntegrationHttpClientRegistry
{
    public static class ClientNames
    {
        // Platform adapters
        public const string Trendyol = "Trendyol";
        public const string OpenCart = "OpenCart";
        public const string Ciceksepeti = "Ciceksepeti";
        public const string HepsiburadaToken = "HepsiburadaToken";
        public const string Hepsiburada = "Hepsiburada";
        public const string Pazarama = "Pazarama";
        public const string PazaramaToken = "PazaramaToken";
        public const string AmazonTr = "AmazonTr";
        public const string AmazonEu = "AmazonEu";
        public const string AmazonFBA = "AmazonFBA";
        public const string AmazonLWA = "AmazonLWA";
        public const string Bitrix24 = "Bitrix24";
        public const string Bitrix24Auth = "Bitrix24Auth";
        public const string Ebay = "Ebay";
        public const string Ozon = "Ozon";
        public const string PttAvm = "PttAvm";
        public const string Shopify = "Shopify";
        public const string WooCommerce = "WooCommerce";
        public const string Etsy = "Etsy";
        public const string Zalando = "Zalando";

        // Cargo adapters
        public const string YurticiKargo = "YurticiKargo";
        public const string ArasKargo = "ArasKargo";
        public const string SuratKargo = "SuratKargo";
        public const string MngKargo = "MngKargo";
        public const string PttKargo = "PttKargo";
        public const string HepsiJet = "HepsiJet";
        public const string Sendeo = "Sendeo";

        // Invoice providers
        public const string Sovos = "Sovos";
        public const string ParasutInvoice = "ParasutInvoice";
        public const string TrendyolEFaturam = "TrendyolEFaturam";
        public const string ELogoSoap = "ELogoSoap";
        public const string ELogo = "ELogo";
        public const string BirFatura = "BirFatura";
        public const string DijitalPlanet = "DijitalPlanet";
        public const string GibPortal = "GibPortal";
        public const string HBFatura = "HBFatura";
        public const string GibPortalEInvoice = "GibPortalEInvoice";

        // Services
        public const string ProductScraper = "ProductScraper";
        public const string ParasutAccounting = "ParasutAccounting";
        public const string FeedHealthCheck = "FeedHealthCheck";
        public const string PayTRDirect = "PayTRDirect";
        public const string PayTRiFrame = "PayTRiFrame";
        public const string Iyzico = "Iyzico";
        public const string Stripe = "Stripe";
        public const string Hepsilojistik = "Hepsilojistik";
    }

    /// <summary>
    /// Registers all named HttpClients for integration adapters.
    /// Call before <see cref="IntegrationServiceRegistration.AddIntegrationServices"/>.
    /// </summary>
    public static IServiceCollection AddIntegrationHttpClients(this IServiceCollection services)
    {
        // Platform adapters — default 100s timeout
        RegisterDefault(services, ClientNames.Trendyol);
        RegisterDefault(services, ClientNames.OpenCart);
        RegisterDefault(services, ClientNames.Ciceksepeti);
        RegisterDefault(services, ClientNames.HepsiburadaToken);
        RegisterDefault(services, ClientNames.Hepsiburada);
        RegisterDefault(services, ClientNames.Pazarama);
        RegisterDefault(services, ClientNames.PazaramaToken);
        RegisterDefault(services, ClientNames.AmazonTr);
        RegisterDefault(services, ClientNames.AmazonEu);
        RegisterDefault(services, ClientNames.AmazonFBA);
        RegisterDefault(services, ClientNames.AmazonLWA);
        RegisterDefault(services, ClientNames.Bitrix24);
        RegisterDefault(services, ClientNames.Bitrix24Auth);
        RegisterDefault(services, ClientNames.Ebay);
        RegisterDefault(services, ClientNames.Ozon);
        RegisterDefault(services, ClientNames.PttAvm);
        RegisterDefault(services, ClientNames.Shopify);
        RegisterDefault(services, ClientNames.WooCommerce);
        RegisterDefault(services, ClientNames.Etsy);
        RegisterDefault(services, ClientNames.Zalando);

        // Cargo adapters
        RegisterDefault(services, ClientNames.YurticiKargo);
        RegisterDefault(services, ClientNames.ArasKargo);
        RegisterDefault(services, ClientNames.SuratKargo);
        RegisterDefault(services, ClientNames.MngKargo);
        RegisterDefault(services, ClientNames.PttKargo);
        RegisterDefault(services, ClientNames.HepsiJet);
        RegisterDefault(services, ClientNames.Sendeo);

        // Invoice providers
        RegisterDefault(services, ClientNames.Sovos);
        RegisterDefault(services, ClientNames.ParasutInvoice);
        RegisterDefault(services, ClientNames.TrendyolEFaturam);
        RegisterDefault(services, ClientNames.ELogoSoap);
        RegisterDefault(services, ClientNames.ELogo);
        RegisterDefault(services, ClientNames.BirFatura);
        RegisterDefault(services, ClientNames.DijitalPlanet);
        RegisterDefault(services, ClientNames.GibPortal);
        RegisterDefault(services, ClientNames.HBFatura);
        services.AddHttpClient(ClientNames.GibPortalEInvoice, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Services
        RegisterDefault(services, ClientNames.ProductScraper);
        services.AddHttpClient(ClientNames.ParasutAccounting, client =>
        {
            client.BaseAddress = new Uri(ParasutApiBaseUrl);
        });
        RegisterDefault(services, ClientNames.FeedHealthCheck);
        RegisterDefault(services, ClientNames.PayTRDirect);
        RegisterDefault(services, ClientNames.PayTRiFrame);
        RegisterDefault(services, ClientNames.Iyzico);
        RegisterDefault(services, ClientNames.Stripe);
        RegisterDefault(services, ClientNames.Hepsilojistik);

        return services;
    }

    /// <summary>
    /// Default timeout for named HttpClients.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);

    private const string ParasutApiBaseUrl = "https://api.parasut.com/v4/";

    /// <summary>
    /// DNS refresh interval — prevents stale DNS cache in long-running services.
    /// Microsoft recommendation: 2 minutes.
    /// </summary>
    private static readonly TimeSpan DnsRefreshTimeout = TimeSpan.FromMinutes(2);

    private static void RegisterDefault(IServiceCollection services, string name)
    {
        services.AddHttpClient(name)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = DnsRefreshTimeout
            });
    }
}
