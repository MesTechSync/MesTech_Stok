using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Soap;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// N11 platform adaptoru — Dalga 5 tam SOAP entegrasyon.
/// IIntegratorAdapter + IOrderCapableAdapter.
/// SimpleSoapClient + N11SoapRequestBuilder kullanan WCF'siz SOAP client.
/// Sayfa bazli pagination (FetchAllPagesAsync), CultureInfo.InvariantCulture.
/// </summary>
public class N11Adapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter,
    IClaimCapableAdapter, ISettlementCapableAdapter, IInvoiceCapableAdapter
{
    private readonly ILogger<N11Adapter> _logger;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(5, 5);
    private SimpleSoapClient? _soapClient;
    private string? _appKey;
    private string? _appSecret;
    private string? _soapBaseUrl;
    private bool _isConfigured;

    private const int DefaultPageSize = 100;
    private const int OrderPageSize = 50;

    // SOAP service URL suffixes
    private const string ProductServicePath = "/ws/ProductService.wsdl";
    private const string ProductSellingServicePath = "/ws/ProductSellingService.wsdl";
    private const string OrderServicePath = "/ws/OrderService.wsdl";
    private const string CategoryServicePath = "/ws/CategoryService.wsdl";
    private const string ShipmentServicePath = "/ws/ShipmentService.wsdl";
    private const string CityServicePath = "/ws/CityService.wsdl";
    private const string InvoiceServicePath = "/ws/InvoiceService.wsdl";
    private const string ClaimServicePath = "/ws/ClaimService.wsdl";
    private const string SettlementServicePath = "/ws/SettlementService.wsdl";
    private const string BrandServicePath = "/ws/BrandService.wsdl";

    public N11Adapter(ILogger<N11Adapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string PlatformCode => nameof(PlatformType.N11);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ── Configuration ────────────────────────────────────

    /// <summary>
    /// Adapter'i N11 SOAP API icin konfigure eder.
    /// </summary>
    public void Configure(string appKey, string appSecret, string soapBaseUrl, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey bos olamaz", nameof(appKey));
        if (string.IsNullOrWhiteSpace(appSecret))
            throw new ArgumentException("appSecret bos olamaz", nameof(appSecret));
        if (string.IsNullOrWhiteSpace(soapBaseUrl))
            throw new ArgumentException("soapBaseUrl bos olamaz", nameof(soapBaseUrl));

        _appKey = appKey;
        _appSecret = appSecret;
        _soapBaseUrl = soapBaseUrl.TrimEnd('/');

        var client = httpClient ?? throw new ArgumentNullException(nameof(httpClient), "HttpClient is required for N11 SOAP communication");
        client.Timeout = TimeSpan.FromSeconds(30);

        _soapClient = new SimpleSoapClient(client, _logger);
        _isConfigured = true;

        _logger.LogInformation("N11Adapter konfigure edildi — BaseUrl={BaseUrl}", _soapBaseUrl);
    }

    private async Task<XElement> ThrottledSoapAsync(
        string url, string soapAction, XElement body, CancellationToken ct)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _soapClient!.SendAsync(url, soapAction, body, ct).ConfigureAwait(false);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured || _soapClient is null)
            throw new InvalidOperationException("N11Adapter henuz konfigure edilmedi. Once Configure() cagirin.");
    }

    // ── Pagination Helper ────────────────────────────────

    private async Task<List<T>> FetchAllPagesAsync<T>(
        Func<int, int, Task<(List<T> items, int totalPages)>> fetcher,
        int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        var all = new List<T>();
        int page = 0, totalPages = 1;
        while (page < totalPages)
        {
            ct.ThrowIfCancellationRequested();
            var (items, total) = await fetcher(page, pageSize).ConfigureAwait(false);
            all.AddRange(items);
            totalPages = total;
            page++;
        }
        return all;
    }

    // ── SOAP Fault Check (namespace-agnostic) ────────────

    /// <summary>
    /// Checks if the SOAP response element is a Fault or contains a Fault.
    /// Uses SimpleSoapClient.ThrowIfFault for descendant search,
    /// plus direct local-name check for the element itself.
    /// </summary>
    private static void ThrowIfSoapFault(XElement response)
    {
        // Check if the element itself is a Fault (SimpleSoapClient returns first child of Body)
        if (response.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase))
        {
            var faultString = response.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value
                ?? "Unknown SOAP Fault";
            throw new InvalidOperationException($"SOAP Fault: {faultString}");
        }

        // Also check descendants (delegating to existing method)
        SimpleSoapClient.ThrowIfFault(response);
    }

    // ── Namespace-agnostic XML helpers ───────────────────

    /// <summary>
    /// Finds all descendant elements matching a local name, ignoring namespace.
    /// </summary>
    private static IEnumerable<XElement> DescendantsByLocalName(XElement root, string localName)
    {
        return root.Descendants().Where(e => e.Name.LocalName == localName);
    }

    /// <summary>
    /// Finds the first child element matching a local name, ignoring namespace.
    /// </summary>
    private static XElement? ElementByLocalName(XElement parent, string localName)
    {
        return parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
    }

    // ── IIntegratorAdapter ───────────────────────────────

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = N11SoapRequestBuilder.BuildSaveProduct(
                _appKey!, _appSecret!,
                productSellerCode: product.SKU,
                title: product.Name,
                categoryId: 0, // Default category — caller should set via platform mapping
                price: product.SalePrice,
                stockQuantity: product.Stock,
                description: product.Description);

            var url = _soapBaseUrl + ProductServicePath;
            var response = await ThrottledSoapAsync(url, "SaveProduct", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var status = GetResultStatus(response);
            if (status == "success")
            {
                _logger.LogInformation("N11 SaveProduct basarili — SKU={SKU}", product.SKU);
                return true;
            }

            _logger.LogWarning("N11 SaveProduct basarisiz — SKU={SKU}, Status={Status}", product.SKU, status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 SaveProduct hatasi — SKU={SKU}", product.SKU);
            return false;
        }
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var products = await FetchAllPagesAsync<Product>(
                async (page, pageSize) =>
                {
                    var body = N11SoapRequestBuilder.BuildGetProducts(_appKey!, _appSecret!, page, pageSize).ConfigureAwait(false);
                    var url = _soapBaseUrl + ProductServicePath;
                    var response = await ThrottledSoapAsync(url, "GetProductList", body, ct).ConfigureAwait(false);

                    ThrowIfSoapFault(response);

                    var items = ParseProducts(response);
                    var totalPages = ParseTotalPages(response);

                    return (items, totalPages);
                },
                pageSize: DefaultPageSize,
                ct).ConfigureAwait(false);

            _logger.LogInformation("N11 PullProducts tamamlandi — {Count} urun cekildi", products.Count);
            return products.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 PullProducts hatasi");
            return Array.Empty<Product>();
        }
    }

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            // N11 uses numeric product ID; we pass Guid hash as long for mapping
            var n11ProductId = Math.Abs(productId.GetHashCode());
            var body = N11SoapRequestBuilder.BuildUpdateStock(_appKey!, _appSecret!, n11ProductId, newStock);
            var url = _soapBaseUrl + ProductServicePath;
            var response = await ThrottledSoapAsync(url, "UpdateStockByStockId", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var status = GetResultStatus(response);
            if (status == "success")
            {
                _logger.LogInformation("N11 StockUpdate basarili — ProductId={ProductId}, NewStock={Stock}",
                    productId, newStock);
                return true;
            }

            _logger.LogWarning("N11 StockUpdate basarisiz — ProductId={ProductId}, Status={Status}",
                productId, status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 StockUpdate hatasi — ProductId={ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var n11ProductId = Math.Abs(productId.GetHashCode());
            var body = N11SoapRequestBuilder.BuildUpdatePrice(_appKey!, _appSecret!, n11ProductId, newPrice);
            var url = _soapBaseUrl + ProductServicePath;
            var response = await ThrottledSoapAsync(url, "UpdateProductPriceById", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var status = GetResultStatus(response);
            if (status == "success")
            {
                _logger.LogInformation("N11 PriceUpdate basarili — ProductId={ProductId}, NewPrice={Price}",
                    productId, newPrice.ToString(CultureInfo.InvariantCulture));
                return true;
            }

            _logger.LogWarning("N11 PriceUpdate basarisiz — ProductId={ProductId}, Status={Status}",
                productId, status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 PriceUpdate hatasi — ProductId={ProductId}", productId);
            return false;
        }
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var appKey = credentials.GetValueOrDefault("N11AppKey", "");
            var appSecret = credentials.GetValueOrDefault("N11AppSecret", "");
            var baseUrl = credentials.GetValueOrDefault("N11BaseUrl", "https://api.n11.com");

            if (string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(appSecret))
            {
                return new ConnectionTestResultDto
                {
                    PlatformCode = PlatformCode,
                    ErrorMessage = "N11AppKey ve N11AppSecret zorunludur",
                    ResponseTime = sw.Elapsed
                };
            }

            // Configure if not already configured or with new credentials
            if (!_isConfigured)
            {
                Configure(appKey, appSecret, baseUrl);
            }

            // Test: fetch top-level categories
            var body = N11SoapRequestBuilder.BuildGetCategories(_appKey!, _appSecret!);
            var url = _soapBaseUrl + CategoryServicePath;
            var response = await ThrottledSoapAsync(url, "GetTopLevelCategories", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            sw.Stop();
            return new ConnectionTestResultDto
            {
                IsSuccess = true,
                PlatformCode = PlatformCode,
                StoreName = "N11 Marketplace",
                ResponseTime = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "N11 TestConnection hatasi");
            return new ConnectionTestResultDto
            {
                PlatformCode = PlatformCode,
                ErrorMessage = ex.Message,
                ResponseTime = sw.Elapsed
            };
        }
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = N11SoapRequestBuilder.BuildGetCategories(_appKey!, _appSecret!);
            var url = _soapBaseUrl + CategoryServicePath;
            var response = await ThrottledSoapAsync(url, "GetTopLevelCategories", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var categories = DescendantsByLocalName(response, "categories")
                .Select(c => new CategoryDto
                {
                    PlatformCategoryId = int.Parse(
                        ElementByLocalName(c, "id")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Name = ElementByLocalName(c, "name")?.Value ?? string.Empty
                }).ToList();

            _logger.LogInformation("N11 GetCategories tamamlandi — {Count} kategori cekildi", categories.Count);
            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 GetCategories hatasi");
            return Array.Empty<CategoryDto>();
        }
    }

    // ── IOrderCapableAdapter ─────────────────────────────

    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var orders = await FetchAllPagesAsync<ExternalOrderDto>(
                async (page, pageSize) =>
                {
                    var body = N11SoapRequestBuilder.BuildGetOrders(
                        _appKey!, _appSecret!, status: null, currentPage: page, pageSize: pageSize).ConfigureAwait(false);
                    var url = _soapBaseUrl + OrderServicePath;
                    var response = await ThrottledSoapAsync(url, "DetailedOrderList", body, ct).ConfigureAwait(false);

                    ThrowIfSoapFault(response);

                    var items = ParseOrders(response);
                    var totalPages = ParseOrderTotalPages(response);

                    return (items, totalPages);
                },
                pageSize: OrderPageSize,
                ct).ConfigureAwait(false);

            // Filter by since date if provided
            if (since.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= since.Value).ToList();
            }

            _logger.LogInformation("N11 PullOrders tamamlandi — {Count} siparis cekildi", orders.Count);
            return orders.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 PullOrders hatasi");
            return Array.Empty<ExternalOrderDto>();
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            if (!long.TryParse(packageId, CultureInfo.InvariantCulture, out var orderItemId))
            {
                _logger.LogWarning("N11 UpdateOrderStatus — gecersiz packageId: {PackageId}", packageId);
                return false;
            }

            var body = N11SoapRequestBuilder.BuildUpdateOrderStatus(_appKey!, _appSecret!, orderItemId, status);
            var url = _soapBaseUrl + OrderServicePath;
            var response = await ThrottledSoapAsync(url, "OrderItemAccept", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var resultStatus = GetResultStatus(response);
            if (resultStatus == "success")
            {
                _logger.LogInformation("N11 UpdateOrderStatus basarili — OrderItemId={Id}, Status={Status}",
                    packageId, status);
                return true;
            }

            _logger.LogWarning("N11 UpdateOrderStatus basarisiz — OrderItemId={Id}, ResultStatus={Status}",
                packageId, resultStatus);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 UpdateOrderStatus hatasi — OrderItemId={Id}", packageId);
            return false;
        }
    }

    // ── IShipmentCapableAdapter ────────────────────────────

    /// <summary>
    /// N11'e kargo bildirimi gonderir (SOAP MakeOrderItemShipment).
    /// platformOrderId: N11 orderItem ID (long formatinda string).
    /// </summary>
    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            if (!long.TryParse(platformOrderId, CultureInfo.InvariantCulture, out var orderItemId))
            {
                _logger.LogWarning("N11 SendShipment — gecersiz platformOrderId: {OrderId}", platformOrderId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                _logger.LogWarning("N11 SendShipment — trackingNumber bos olamaz. OrderId={OrderId}", platformOrderId);
                return false;
            }

            var shipmentCompany = MapCargoProviderToN11(provider);

            var body = N11SoapRequestBuilder.BuildUpdateShipment(
                _appKey!, _appSecret!, orderItemId, shipmentCompany, trackingNumber);

            var url = _soapBaseUrl + ShipmentServicePath;
            var response = await ThrottledSoapAsync(url, "MakeOrderItemShipment", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var status = GetResultStatus(response);
            if (status == "success")
            {
                _logger.LogInformation(
                    "N11 SendShipment basarili — OrderItemId={OrderItemId}, Tracking={Tracking}, Cargo={Cargo}",
                    platformOrderId, trackingNumber, shipmentCompany);
                return true;
            }

            _logger.LogWarning(
                "N11 SendShipment basarisiz — OrderItemId={OrderItemId}, Status={Status}",
                platformOrderId, status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 SendShipment hatasi — OrderItemId={OrderItemId}", platformOrderId);
            return false;
        }
    }

    /// <summary>
    /// CargoProvider enum degerini N11'in beklediği kargo firma adina cevirir.
    /// </summary>
    private static string MapCargoProviderToN11(CargoProvider provider) => provider switch
    {
        CargoProvider.YurticiKargo => "Yurtiçi Kargo",
        CargoProvider.ArasKargo => "Aras Kargo",
        CargoProvider.SuratKargo => "Sürat Kargo",
        CargoProvider.MngKargo => "MNG Kargo",
        CargoProvider.PttKargo => "PTT Kargo",
        CargoProvider.Hepsijet => "HepsiJet",
        CargoProvider.UPS => "UPS",
        CargoProvider.Sendeo => "Sendeo",
        _ => provider.ToString()
    };

    // ── ProductSellingService ─────────────────────────────

    /// <summary>
    /// N11'de urunu satisa acar (ProductSellingService → activateProductSelling).
    /// </summary>
    public async Task<bool> ActivateProductSellingAsync(long productId, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = N11SoapRequestBuilder.BuildActivateProductSelling(_appKey!, _appSecret!, productId);
            var url = _soapBaseUrl + ProductSellingServicePath;
            var response = await ThrottledSoapAsync(url, "ActivateProductSelling", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var status = GetResultStatus(response);
            if (status == "success")
            {
                _logger.LogInformation("N11 ActivateProductSelling basarili — ProductId={ProductId}", productId);
                return true;
            }

            _logger.LogWarning("N11 ActivateProductSelling basarisiz — ProductId={ProductId}, Status={Status}",
                productId, status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 ActivateProductSelling hatasi — ProductId={ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// N11'de urunu satisdan kaldirir (ProductSellingService → deactivateProductSelling).
    /// </summary>
    public async Task<bool> DeactivateProductSellingAsync(long productId, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = N11SoapRequestBuilder.BuildDeactivateProductSelling(_appKey!, _appSecret!, productId);
            var url = _soapBaseUrl + ProductSellingServicePath;
            var response = await ThrottledSoapAsync(url, "DeactivateProductSelling", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var status = GetResultStatus(response);
            if (status == "success")
            {
                _logger.LogInformation("N11 DeactivateProductSelling basarili — ProductId={ProductId}", productId);
                return true;
            }

            _logger.LogWarning("N11 DeactivateProductSelling basarisiz — ProductId={ProductId}, Status={Status}",
                productId, status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 DeactivateProductSelling hatasi — ProductId={ProductId}", productId);
            return false;
        }
    }

    // ── IInvoiceCapableAdapter ──────────────────────────

    /// <summary>
    /// N11'e fatura bildirimi gonderir (InvoiceService → sendInvoice).
    /// IInvoiceCapableAdapter.SendInvoiceLinkAsync — N11 link-based invoice gonderimini desteklemez,
    /// dogrudan SendInvoiceAsync kullanin.
    /// </summary>
    public Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    {
        // N11 link-based invoice gonderimini desteklemez — direkt SOAP ile numara gonderilir
        _logger.LogWarning("N11 SendInvoiceLinkAsync desteklenmiyor, SendInvoiceAsync kullanin.");
        return Task.FromResult(false);
    }

    /// <summary>
    /// N11'e PDF fatura dosyasi gonderir — N11 dosya bazli fatura gonderimini desteklemez.
    /// </summary>
    public Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    {
        _logger.LogWarning("N11 SendInvoiceFileAsync desteklenmiyor, SendInvoiceAsync kullanin.");
        return Task.FromResult(false);
    }

    /// <summary>
    /// N11'e SOAP ile fatura bildirimi gonderir (InvoiceService → sendInvoice).
    /// </summary>
    public async Task<bool> SendInvoiceAsync(long orderId, string invoiceNo, DateTime invoiceDate,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = N11SoapRequestBuilder.BuildSendInvoice(_appKey!, _appSecret!, orderId, invoiceNo, invoiceDate);
            var url = _soapBaseUrl + InvoiceServicePath;
            var response = await ThrottledSoapAsync(url, "SendInvoice", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var status = GetResultStatus(response);
            if (status == "success")
            {
                _logger.LogInformation("N11 SendInvoice basarili — OrderId={OrderId}, InvoiceNo={InvoiceNo}",
                    orderId, invoiceNo);
                return true;
            }

            _logger.LogWarning("N11 SendInvoice basarisiz — OrderId={OrderId}, Status={Status}",
                orderId, status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 SendInvoice hatasi — OrderId={OrderId}", orderId);
            return false;
        }
    }

    // ── IClaimCapableAdapter ────────────────────────────

    /// <summary>
    /// N11'den iade taleplerini cekilir (ClaimService → getClaims).
    /// </summary>
    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var claims = await FetchAllPagesAsync<ExternalClaimDto>(
                async (page, pageSize) =>
                {
                    var body = N11SoapRequestBuilder.BuildGetClaims(_appKey!, _appSecret!, page, pageSize).ConfigureAwait(false);
                    var url = _soapBaseUrl + ClaimServicePath;
                    var response = await ThrottledSoapAsync(url, "GetClaims", body, ct).ConfigureAwait(false);

                    ThrowIfSoapFault(response);

                    var items = ParseClaims(response);
                    var totalPages = ParseTotalPages(response);

                    return (items, totalPages);
                },
                pageSize: OrderPageSize,
                ct).ConfigureAwait(false);

            if (since.HasValue)
            {
                claims = claims.Where(c => c.ClaimDate >= since.Value).ToList();
            }

            _logger.LogInformation("N11 PullClaims tamamlandi — {Count} iade talebi cekildi", claims.Count);
            return claims.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 PullClaims hatasi");
            return Array.Empty<ExternalClaimDto>();
        }
    }

    /// <summary>
    /// N11'de iade talebini onaylar (ClaimService → approveClaim).
    /// </summary>
    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            if (!long.TryParse(claimId, CultureInfo.InvariantCulture, out var claimIdLong))
            {
                _logger.LogWarning("N11 ApproveClaim — gecersiz claimId: {ClaimId}", claimId);
                return false;
            }

            var body = N11SoapRequestBuilder.BuildApproveClaim(_appKey!, _appSecret!, claimIdLong);
            var url = _soapBaseUrl + ClaimServicePath;
            var response = await ThrottledSoapAsync(url, "ApproveClaim", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var status = GetResultStatus(response);
            if (status == "success")
            {
                _logger.LogInformation("N11 ApproveClaim basarili — ClaimId={ClaimId}", claimId);
                return true;
            }

            _logger.LogWarning("N11 ApproveClaim basarisiz — ClaimId={ClaimId}, Status={Status}",
                claimId, status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 ApproveClaim hatasi — ClaimId={ClaimId}", claimId);
            return false;
        }
    }

    /// <summary>
    /// N11'de iade talebini reddeder — N11 SOAP API reject endpoint'i desteklemez.
    /// </summary>
    public Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        _logger.LogWarning("N11 RejectClaimAsync — N11 SOAP API reject endpoint'i desteklemiyor. ClaimId={ClaimId}", claimId);
        return Task.FromResult(false);
    }

    // ── ISettlementCapableAdapter ───────────────────────

    /// <summary>
    /// N11'den cari hesap ekstresi cekilir (SettlementService → getSettlements).
    /// </summary>
    public async Task<SettlementDto?> GetSettlementAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = N11SoapRequestBuilder.BuildGetSettlements(_appKey!, _appSecret!, startDate, endDate);
            var url = _soapBaseUrl + SettlementServicePath;
            var response = await ThrottledSoapAsync(url, "GetSettlements", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var settlement = ParseSettlement(response, startDate, endDate);

            _logger.LogInformation("N11 GetSettlement tamamlandi — {Start:d} ~ {End:d}, Net={Net:N2} TL",
                startDate, endDate, settlement.NetAmount);
            return settlement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 GetSettlement hatasi — {Start:d} ~ {End:d}", startDate, endDate);
            return null;
        }
    }

    /// <summary>
    /// N11 kargo faturalari — N11 SOAP API bu endpoint'i ayri sunmaz.
    /// Settlement icerisindeki kargo verileri kullanilir.
    /// </summary>
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(
        DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogWarning("N11 GetCargoInvoicesAsync — N11 SOAP API kargo faturasi endpoint'i sunmuyor.");
        return Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());
    }

    // ── CategoryService (attributes) ────────────────────

    /// <summary>
    /// N11'den kategori ozelliklerini cekilir (CategoryService → getCategoryAttributes).
    /// </summary>
    public async Task<IReadOnlyList<CategoryAttributeDto>> GetCategoryAttributesAsync(
        long categoryId, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = N11SoapRequestBuilder.BuildGetCategoryAttributes(_appKey!, _appSecret!, categoryId);
            var url = _soapBaseUrl + CategoryServicePath;
            var response = await ThrottledSoapAsync(url, "GetCategoryAttributes", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var attributes = DescendantsByLocalName(response, "attribute")
                .Select(a => new CategoryAttributeDto
                {
                    AttributeId = int.Parse(
                        ElementByLocalName(a, "id")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Name = ElementByLocalName(a, "name")?.Value ?? string.Empty,
                    Required = string.Equals(
                        ElementByLocalName(a, "mandatory")?.Value, "true", StringComparison.OrdinalIgnoreCase),
                    AllowCustom = string.Equals(
                        ElementByLocalName(a, "multipleSelect")?.Value, "true", StringComparison.OrdinalIgnoreCase),
                    Values = DescendantsByLocalName(a, "value")
                        .Select(v => new CategoryAttributeValueDto
                        {
                            Id = int.Parse(
                                ElementByLocalName(v, "id")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Name = ElementByLocalName(v, "name")?.Value ?? string.Empty
                        }).ToList()
                }).ToList();

            _logger.LogInformation("N11 GetCategoryAttributes tamamlandi — CategoryId={CategoryId}, {Count} ozellik",
                categoryId, attributes.Count);
            return attributes.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 GetCategoryAttributes hatasi — CategoryId={CategoryId}", categoryId);
            return Array.Empty<CategoryAttributeDto>();
        }
    }

    // ── BrandService ────────────────────────────────────

    /// <summary>
    /// N11'den marka listesini cekilir (BrandService → getBrands).
    /// </summary>
    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(
        int page = 0, int pageSize = DefaultPageSize, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = N11SoapRequestBuilder.BuildGetBrands(_appKey!, _appSecret!, page, pageSize);
            var url = _soapBaseUrl + BrandServicePath;
            var response = await ThrottledSoapAsync(url, "GetBrands", body, ct).ConfigureAwait(false);

            ThrowIfSoapFault(response);

            var brands = DescendantsByLocalName(response, "brand")
                .Select(b => new BrandDto
                {
                    PlatformBrandId = int.Parse(
                        ElementByLocalName(b, "id")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Name = ElementByLocalName(b, "name")?.Value ?? string.Empty
                }).ToList();

            _logger.LogInformation("N11 GetBrands tamamlandi — {Count} marka cekildi", brands.Count);
            return brands.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "N11 GetBrands hatasi");
            return Array.Empty<BrandDto>();
        }
    }

    // ── XML Response Parsing Helpers ─────────────────────

    private static string GetResultStatus(XElement response)
    {
        var resultEl = DescendantsByLocalName(response, "result").FirstOrDefault();
        return ElementByLocalName(resultEl!, "status")?.Value ?? "unknown";
    }

    private static int ParseTotalPages(XElement response)
    {
        // N11 products: pagingData/totalPage or productList/totalCount
        var pagingData = DescendantsByLocalName(response, "pagingData").FirstOrDefault();
        if (pagingData is not null)
        {
            var totalPageEl = ElementByLocalName(pagingData, "totalPage");
            if (totalPageEl is not null)
                return int.Parse(totalPageEl.Value, CultureInfo.InvariantCulture);
        }

        // Fallback: calculate from totalCount / pageSize in productList
        var productList = DescendantsByLocalName(response, "productList").FirstOrDefault();
        if (productList is not null)
        {
            var totalCount = int.Parse(
                ElementByLocalName(productList, "totalCount")?.Value ?? "0", CultureInfo.InvariantCulture);
            var pageSize = int.Parse(
                ElementByLocalName(productList, "pageSize")?.Value ?? DefaultPageSize.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

            return pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
        }

        return 1;
    }

    private static List<Product> ParseProducts(XElement response)
    {
        // Products are in <products> elements (either under productList or directly)
        return DescendantsByLocalName(response, "products")
            .Where(p => ElementByLocalName(p, "productSellerCode") is not null)
            .Select(p =>
            {
                var product = new Product
                {
                    SKU = ElementByLocalName(p, "productSellerCode")?.Value ?? string.Empty,
                    Name = ElementByLocalName(p, "title")?.Value ?? string.Empty
                };

                var priceValue = ElementByLocalName(p, "price")?.Value;
                if (!string.IsNullOrEmpty(priceValue) &&
                    decimal.TryParse(priceValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    product.SalePrice = price;
                }

                var stockItem = DescendantsByLocalName(p, "stockItem").FirstOrDefault();
                var stockQuantity = stockItem is not null
                    ? ElementByLocalName(stockItem, "quantity")?.Value
                    : null;
                if (!string.IsNullOrEmpty(stockQuantity) &&
                    int.TryParse(stockQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                {
                    product.Stock = qty;
                }

                return product;
            }).ToList();
    }

    private static int ParseOrderTotalPages(XElement response)
    {
        var pagingData = DescendantsByLocalName(response, "pagingData").FirstOrDefault();
        if (pagingData is null)
            return 1;

        var totalPage = ElementByLocalName(pagingData, "totalPage");
        if (totalPage is not null)
            return int.Parse(totalPage.Value, CultureInfo.InvariantCulture);

        var totalCount = ElementByLocalName(pagingData, "totalCount");
        if (totalCount is not null)
        {
            var count = int.Parse(totalCount.Value, CultureInfo.InvariantCulture);
            return count > 0 ? (int)Math.Ceiling(count / (double)OrderPageSize) : 1;
        }

        return 1;
    }

    private List<ExternalOrderDto> ParseOrders(XElement response)
    {
        return DescendantsByLocalName(response, "orderList")
            .Where(o => ElementByLocalName(o, "id") is not null)
            .Select(o =>
            {
                var buyer = ElementByLocalName(o, "buyer");
                var shippingAddr = ElementByLocalName(o, "shippingAddress");

                var order = new ExternalOrderDto
                {
                    PlatformCode = PlatformCode,
                    PlatformOrderId = ElementByLocalName(o, "id")?.Value ?? string.Empty,
                    OrderNumber = ElementByLocalName(o, "orderNumber")?.Value
                        ?? ElementByLocalName(o, "id")?.Value ?? string.Empty,
                    Status = ElementByLocalName(o, "status")?.Value ?? string.Empty,
                    CustomerName = buyer is not null
                        ? ElementByLocalName(buyer, "fullName")?.Value ?? string.Empty
                        : string.Empty,
                    CustomerEmail = buyer is not null
                        ? ElementByLocalName(buyer, "email")?.Value
                        : null,
                    CustomerPhone = buyer is not null
                        ? ElementByLocalName(buyer, "phone")?.Value
                        : null,
                    CustomerAddress = shippingAddr is not null
                        ? ElementByLocalName(shippingAddr, "address")?.Value
                        : null,
                    CustomerCity = shippingAddr is not null
                        ? ElementByLocalName(shippingAddr, "city")?.Value
                        : null,
                    Currency = "TRY"
                };

                // Parse order date
                var createDate = ElementByLocalName(o, "createDate")?.Value;
                if (!string.IsNullOrEmpty(createDate) &&
                    DateTime.TryParse(createDate, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var dt))
                {
                    order.OrderDate = dt;
                }

                // Parse total amount
                var totalAmount = ElementByLocalName(o, "totalAmount")?.Value;
                if (!string.IsNullOrEmpty(totalAmount) &&
                    decimal.TryParse(totalAmount, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out var amount))
                {
                    order.TotalAmount = amount;
                }

                // Parse order items
                var items = DescendantsByLocalName(o, "orderItem").Select(item => new ExternalOrderLineDto
                {
                    PlatformLineId = ElementByLocalName(item, "id")?.Value,
                    SKU = ElementByLocalName(item, "sellerCode")?.Value,
                    ProductName = ElementByLocalName(item, "productName")?.Value ?? string.Empty,
                    Quantity = int.TryParse(ElementByLocalName(item, "quantity")?.Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var q) ? q : 1,
                    UnitPrice = decimal.TryParse(ElementByLocalName(item, "price")?.Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0m,
                    LineTotal = decimal.TryParse(ElementByLocalName(item, "totalPrice")?.Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var lt) ? lt : 0m
                }).ToList();

                order.Lines = items;

                return order;
            }).ToList();
    }

    private List<ExternalClaimDto> ParseClaims(XElement response)
    {
        return DescendantsByLocalName(response, "claim")
            .Where(c => ElementByLocalName(c, "id") is not null)
            .Select(c =>
            {
                var claim = new ExternalClaimDto
                {
                    PlatformCode = PlatformCode,
                    PlatformClaimId = ElementByLocalName(c, "id")?.Value ?? string.Empty,
                    OrderNumber = ElementByLocalName(c, "orderNumber")?.Value ?? string.Empty,
                    Status = ElementByLocalName(c, "status")?.Value ?? string.Empty,
                    Reason = ElementByLocalName(c, "reason")?.Value ?? string.Empty,
                    ReasonDetail = ElementByLocalName(c, "reasonDetail")?.Value,
                    CustomerName = ElementByLocalName(c, "buyerName")?.Value ?? string.Empty,
                    Currency = "TRY"
                };

                var amountStr = ElementByLocalName(c, "amount")?.Value;
                if (!string.IsNullOrEmpty(amountStr) &&
                    decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                {
                    claim.Amount = amount;
                }

                var dateStr = ElementByLocalName(c, "createDate")?.Value;
                if (!string.IsNullOrEmpty(dateStr) &&
                    DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    claim.ClaimDate = dt;
                }

                return claim;
            }).ToList();
    }

    private SettlementDto ParseSettlement(XElement response, DateTime startDate, DateTime endDate)
    {
        var settlement = new SettlementDto
        {
            PlatformCode = PlatformCode,
            StartDate = startDate,
            EndDate = endDate,
            Currency = "TRY"
        };

        var lines = DescendantsByLocalName(response, "settlement")
            .Select(s =>
            {
                var line = new SettlementLineDto
                {
                    OrderNumber = ElementByLocalName(s, "orderNumber")?.Value,
                    TransactionType = ElementByLocalName(s, "transactionType")?.Value
                };

                var amountStr = ElementByLocalName(s, "amount")?.Value;
                if (!string.IsNullOrEmpty(amountStr) &&
                    decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                {
                    line.Amount = amount;
                }

                var commStr = ElementByLocalName(s, "commissionAmount")?.Value;
                if (!string.IsNullOrEmpty(commStr) &&
                    decimal.TryParse(commStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var comm))
                {
                    line.CommissionAmount = comm;
                }

                var dateStr = ElementByLocalName(s, "transactionDate")?.Value;
                if (!string.IsNullOrEmpty(dateStr) &&
                    DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    line.TransactionDate = dt;
                }

                return line;
            }).ToList();

        settlement.Lines = lines;
        settlement.TotalSales = lines.Where(l => l.TransactionType == "Sale").Sum(l => l.Amount);
        settlement.TotalCommission = lines.Sum(l => l.CommissionAmount ?? 0);
        settlement.NetAmount = lines.Sum(l => l.Amount) - settlement.TotalCommission;

        return settlement;
    }
}
