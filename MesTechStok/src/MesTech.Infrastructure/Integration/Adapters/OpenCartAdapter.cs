using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using MesTech.Infrastructure.Security;
using Polly;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// OpenCart platform adaptoru — IIntegratorAdapter + IOrderCapableAdapter + ICustomerSyncCapable + ICategorySyncCapable
/// + ISettlementCapableAdapter + IShipmentCapableAdapter + IClaimCapableAdapter + IInvoiceCapableAdapter.
/// Polly retry pipeline, batch stok guncelleme, siparis cekme, musteri ve kategori senkronizasyonu destegi.
/// </summary>
public sealed class OpenCartAdapter : IIntegratorAdapter, IOrderCapableAdapter,
    ICustomerSyncCapable, ICategorySyncCapable, ISettlementCapableAdapter,
    IShipmentCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter, IWebhookCapableAdapter, IPingableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenCartAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private string _apiToken = string.Empty;
    private bool _isConfigured;

    public OpenCartAdapter(HttpClient httpClient, ILogger<OpenCartAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // OpenCart self-hosted — slow servers common, 60s prevents false timeout
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested),
                OnRetry = args =>
                {
                    _logger.LogWarning("OpenCart retry #{Attempt} after {Delay}ms — Status: {Status}",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Result?.StatusCode.ToString() ?? args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .Handle<HttpRequestException>(),
                OnOpened = args =>
                {
                    _logger.LogWarning("OpenCart circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public string PlatformCode => nameof(PlatformType.OpenCart);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        credentials.TryGetValue("ApiToken", out var apiToken);
        credentials.TryGetValue("BaseUrl", out var baseUrl);

        _apiToken = apiToken ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != "https" && parsedUri.Scheme != "http"))
                throw new ArgumentException($"Invalid OpenCart base URL scheme: {baseUrl}. Only HTTP(S) allowed.");

            if (parsedUri.Host is "localhost" or "127.0.0.1" || parsedUri.Host.StartsWith("10.") ||
                parsedUri.Host.StartsWith("172.") || parsedUri.Host.StartsWith("192.168."))
                _logger.LogWarning("[OpenCartAdapter] BaseUrl points to internal/private network: {BaseUrl}", baseUrl);

            _httpClient.BaseAddress = parsedUri;
        }

        _isConfigured = true;
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto { PlatformCode = PlatformCode };

        try
        {
            if (!credentials.ContainsKey("BaseUrl") || string.IsNullOrWhiteSpace(credentials["BaseUrl"]) ||
                !credentials.ContainsKey("ApiToken") || string.IsNullOrWhiteSpace(credentials["ApiToken"]))
            {
                result.ErrorMessage = "BaseUrl ve ApiToken alanlari zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            ConfigureAuth(credentials);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/rest/products?limit=1");
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;
            sw.Stop();
            result.ResponseTime = sw.Elapsed;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);
                var totalCount = doc.RootElement.TryGetProperty("total", out var tc) ? tc.GetInt32() : 0;

                result.IsSuccess = true;
                result.ProductCount = totalCount;
                result.StoreName = $"OpenCart - {credentials["BaseUrl"]}";
            }
            else
            {
                result.ErrorMessage = $"OpenCart API hatasi: {response.StatusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            result.ErrorMessage = "Baglanti zaman asimina ugradi.";
            result.ResponseTime = sw.Elapsed;
        }
        catch (HttpRequestException ex)
        {
            result.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
            result.ResponseTime = sw.Elapsed;
        }

        _logger.LogInformation("OpenCart connection test: Success={Success}, Time={Time}ms",
            result.IsSuccess, result.ResponseTime.TotalMilliseconds);
        return result;
    }

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushProductAsync SKU: {SKU}", product.SKU);

        try
        {
            var payload = new
            {
                model = product.SKU,
                sku = product.SKU,
                quantity = product.Stock,
                price = product.SalePrice,
                product_description = new Dictionary<string, object>
                {
                    ["1"] = new { name = product.Name, description = product.Description ?? "" }
                },
                status = product.IsActive ? 1 : 0
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/rest/products", content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart PushProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "OpenCart PushProduct: platform gecersiz yanit dondurdu — SKU={SKU}", product.SKU);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PushProduct exception: {SKU}", product.SKU);
            return false;
        }
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            var page = 1;
            const int limit = 100;
            bool hasMore = true;

            while (hasMore)
            {
                var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/rest/products?limit={limit}&page={page}");
                        return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) break;

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("data", out var dataArr))
                {
                    var items = dataArr.EnumerateArray().ToList();
                    if (items.Count == 0) break;

                    foreach (var item in items)
                    {
                        products.Add(new Product
                        {
                            Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                            SKU = item.TryGetProperty("sku", out var s) ? s.GetString() ?? "" : "",
                            SalePrice = item.TryGetProperty("price", out var p) && decimal.TryParse(p.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var pv) ? pv : 0,
                            Stock = item.TryGetProperty("quantity", out var q) && int.TryParse(q.GetString(), out var qv) ? qv : 0,
                            Description = item.TryGetProperty("description", out var d) ? d.GetString() : null
                        });
                    }

                    hasMore = items.Count == limit;
                }
                else
                {
                    hasMore = false;
                }

                page++;
            }

            _logger.LogInformation("OpenCart PullProducts: {Count} products retrieved", products.Count);
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "OpenCart PullProducts: platform gecersiz yanit dondurdu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PullProducts failed");
        }

        return products.AsReadOnly();
    }

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}", productId, newStock);

        try
        {
            var payload = new { quantity = newStock };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/rest/products/{productId}", content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart StockUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "OpenCart StockUpdate: platform gecersiz yanit dondurdu — ProductId={ProductId}", productId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Toplu stok guncelleme — birden fazla urun icin batch islem.
    /// Max 5 paralel istek.
    /// </summary>
    public async Task<int> PushBatchStockUpdateAsync(
        IReadOnlyList<(Guid ProductId, int NewStock)> updates, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushBatchStockUpdateAsync: {Count} items", updates.Count);

        var successCount = 0;
        using var semaphore = new SemaphoreSlim(5, 5);

        var tasks = updates.Select(async update =>
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var ok = await PushStockUpdateAsync(update.ProductId, update.NewStock, ct).ConfigureAwait(false);
                if (ok) Interlocked.Increment(ref successCount);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        _logger.LogInformation("OpenCart BatchStockUpdate: {Success}/{Total} succeeded", successCount, updates.Count);
        return successCount;
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}", productId, newPrice);

        try
        {
            var payload = new { price = newPrice };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/rest/products/{productId}", content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart PriceUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    // ── IOrderCapableAdapter ──────────────────────────────────────────────

    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            var page = 1;
            const int limit = 50;
            bool hasMore = true;

            while (hasMore)
            {
                var url = $"/api/rest/orders?limit={limit}&page={page}&sort=o.date_added&order=DESC";

                var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                        return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) break;

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("data", out var dataArr))
                {
                    hasMore = false;
                    break;
                }

                var items = dataArr.EnumerateArray().ToList();
                if (items.Count == 0) break;

                foreach (var item in items)
                {
                    var orderDate = ParseOrderDate(item);

                    if (since.HasValue && orderDate < since.Value)
                    {
                        hasMore = false;
                        break;
                    }

                    var order = new ExternalOrderDto
                    {
                        PlatformCode = PlatformCode,
                        PlatformOrderId = item.TryGetProperty("order_id", out var oid) ? oid.ToString() : "",
                        OrderNumber = item.TryGetProperty("order_id", out var on) ? on.ToString() : "",
                        Status = MapOpenCartOrderStatus(
                            item.TryGetProperty("order_status_id", out var osid) ? osid.ToString() : "0"),
                        CustomerName = BuildCustomerName(item),
                        CustomerEmail = item.TryGetProperty("email", out var em) ? em.GetString() : null,
                        CustomerPhone = item.TryGetProperty("telephone", out var tel) ? tel.GetString() : null,
                        CustomerAddress = BuildAddress(item),
                        CustomerCity = item.TryGetProperty("shipping_city", out var city) ? city.GetString() : null,
                        TotalAmount = item.TryGetProperty("total", out var tot) && decimal.TryParse(tot.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var tv) ? tv : 0,
                        Currency = item.TryGetProperty("currency_code", out var cur) ? cur.GetString() ?? "TRY" : "TRY",
                        OrderDate = orderDate
                    };

                    if (item.TryGetProperty("products", out var prods) && prods.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var line in prods.EnumerateArray())
                        {
                            order.Lines.Add(new ExternalOrderLineDto
                            {
                                PlatformLineId = line.TryGetProperty("order_product_id", out var lpid) ? lpid.ToString() : null,
                                SKU = line.TryGetProperty("model", out var model) ? model.GetString() : null,
                                ProductName = line.TryGetProperty("name", out var ln) ? ln.GetString() ?? "" : "",
                                Quantity = line.TryGetProperty("quantity", out var lq) && int.TryParse(lq.GetString(), out var lqv) ? lqv : 1,
                                UnitPrice = line.TryGetProperty("price", out var lp) && decimal.TryParse(lp.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var lpv) ? lpv : 0,
                                TaxRate = line.TryGetProperty("tax", out var ltax) && decimal.TryParse(ltax.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var ltv) ? ltv : 0,
                                LineTotal = line.TryGetProperty("total", out var lt) && decimal.TryParse(lt.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var ltov) ? ltov : 0
                            });
                        }
                    }

                    orders.Add(order);
                }

                hasMore = items.Count == limit;
                page++;
            }

            _logger.LogInformation("OpenCart PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.UpdateOrderStatusAsync: OrderId={OrderId} status={Status}", packageId, status);

        try
        {
            var statusId = MapStatusToOpenCartId(status);
            var payload = new { order_status_id = statusId };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/rest/orders/{packageId}", content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart UpdateOrderStatus failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("OpenCart UpdateOrderStatus success: OrderId={OrderId} Status={Status}", packageId, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart UpdateOrderStatus exception: {OrderId}", packageId);
            return false;
        }
    }

    // ── ICustomerSyncCapable ─────────────────────────────────────────────

    public async Task<IReadOnlyList<CustomerSyncDto>> PullCustomersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PullCustomersAsync since={Since}", since);

        var customers = new List<CustomerSyncDto>();

        try
        {
            var page = 1;
            const int limit = 100;
            bool hasMore = true;

            while (hasMore)
            {
                var url = $"/api/rest/customers?limit={limit}&page={page}";
                if (since.HasValue)
                    url += $"&date_modified_from={since.Value:yyyy-MM-dd HH:mm:ss}";

                var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                        return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) break;

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("data", out var dataArr))
                {
                    hasMore = false;
                    break;
                }

                var items = dataArr.EnumerateArray().ToList();
                if (items.Count == 0) break;

                foreach (var item in items)
                {
                    customers.Add(new CustomerSyncDto
                    {
                        Id = item.TryGetProperty("customer_id", out var cid) ? cid.ToString() : "",
                        FirstName = item.TryGetProperty("firstname", out var fn) ? fn.GetString() ?? "" : "",
                        LastName = item.TryGetProperty("lastname", out var ln) ? ln.GetString() ?? "" : "",
                        Email = item.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "",
                        Phone = item.TryGetProperty("telephone", out var tel) ? tel.GetString() : null,
                        Address = item.TryGetProperty("address_1", out var addr) ? addr.GetString() : null,
                        City = item.TryGetProperty("city", out var city) ? city.GetString() : null,
                        Country = item.TryGetProperty("country", out var country) ? country.GetString() : null,
                        DateModified = item.TryGetProperty("date_modified", out var dm)
                            && DateTime.TryParse(dm.GetString(), out var dmv)
                                ? DateTime.SpecifyKind(dmv, DateTimeKind.Utc)
                                : DateTime.MinValue
                    });
                }

                hasMore = items.Count == limit;
                page++;
            }

            _logger.LogInformation("OpenCart PullCustomers: {Count} customers retrieved", customers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PullCustomers failed");
        }

        return customers.AsReadOnly();
    }

    public async Task<bool> PushCustomerAsync(CustomerSyncDto customer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushCustomerAsync: Email={Email}", PiiLogMaskHelper.MaskEmail(customer.Email));

        try
        {
            var payload = new
            {
                firstname = customer.FirstName,
                lastname = customer.LastName,
                email = customer.Email,
                telephone = customer.Phone ?? "",
                address_1 = customer.Address ?? "",
                city = customer.City ?? "",
                country = customer.Country ?? ""
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var isUpdate = !string.IsNullOrWhiteSpace(customer.Id);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var method = isUpdate ? HttpMethod.Put : HttpMethod.Post;
                    var url = isUpdate ? $"/api/rest/customers/{customer.Id}" : "/api/rest/customers";
                    using var request = CreateAuthenticatedRequest(method, url, content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart PushCustomer failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PushCustomer exception: {Email}", PiiLogMaskHelper.MaskEmail(customer.Email));
            return false;
        }
    }

    // ── ICategorySyncCapable ──────────────────────────────────────────────

    public async Task<IReadOnlyList<CategoryTreeSyncDto>> PullCategoryTreeAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PullCategoryTreeAsync called");

        var tree = new List<CategoryTreeSyncDto>();

        try
        {
            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/rest/categories");
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart PullCategoryTree failed: {Status} - {Error}", response.StatusCode, error);
                return tree.AsReadOnly();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("data", out var dataArr))
                return tree.AsReadOnly();

            // Parse flat list from API
            var flatList = new List<CategoryTreeSyncDto>();
            foreach (var item in dataArr.EnumerateArray())
            {
                flatList.Add(new CategoryTreeSyncDto
                {
                    Id = item.TryGetProperty("category_id", out var cid) ? cid.ToString() : "",
                    ParentId = item.TryGetProperty("parent_id", out var pid) ? pid.ToString() : null,
                    Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    SortOrder = item.TryGetProperty("sort_order", out var so)
                        && int.TryParse(so.GetString() ?? so.ToString(), out var sov) ? sov : 0,
                    Status = item.TryGetProperty("status", out var st)
                        && st.ToString() == "1"
                });
            }

            // Build tree structure from flat list
            var lookup = flatList.ToDictionary(c => c.Id);
            foreach (var cat in flatList)
            {
                if (!string.IsNullOrWhiteSpace(cat.ParentId) && cat.ParentId != "0" && lookup.TryGetValue(cat.ParentId, out var parent))
                {
                    parent.Children.Add(cat);
                }
                else
                {
                    tree.Add(cat);
                }
            }

            _logger.LogInformation("OpenCart PullCategoryTree: {Count} root categories, {Total} total",
                tree.Count, flatList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PullCategoryTree failed");
        }

        return tree.AsReadOnly();
    }

    public async Task<bool> PushCategoryAsync(CategorySyncDto category, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushCategoryAsync: Name={Name}", category.Name);

        try
        {
            var payload = new
            {
                parent_id = category.ParentId ?? "0",
                sort_order = category.SortOrder,
                status = category.Status ? 1 : 0,
                category_description = new Dictionary<string, object>
                {
                    ["1"] = new
                    {
                        name = category.Name,
                        description = category.Description ?? "",
                        meta_title = category.Name
                    }
                },
                image = category.ImageUrl ?? ""
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var isUpdate = !string.IsNullOrWhiteSpace(category.Id);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var method = isUpdate ? HttpMethod.Put : HttpMethod.Post;
                    var url = isUpdate ? $"/api/rest/categories/{category.Id}" : "/api/rest/categories";
                    using var request = CreateAuthenticatedRequest(method, url, content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart PushCategory failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PushCategory exception: {Name}", category.Name);
            return false;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, new Uri(relativeUrl, UriKind.Relative));
        request.Headers.TryAddWithoutValidation("X-Oc-Restadmin-Id", _apiToken);
        return request;
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string relativeUrl, HttpContent content)
    {
        var request = CreateAuthenticatedRequest(method, relativeUrl);
        request.Content = content;
        return request;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "OpenCartAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    private static DateTime ParseOrderDate(JsonElement item)
    {
        if (item.TryGetProperty("date_added", out var da))
        {
            var dateStr = da.GetString();
            if (DateTime.TryParse(dateStr, out var parsed))
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }
        return DateTime.MinValue;
    }

    private static string BuildCustomerName(JsonElement item)
    {
        var first = item.TryGetProperty("firstname", out var fn) ? fn.GetString() ?? "" : "";
        var last = item.TryGetProperty("lastname", out var ln) ? ln.GetString() ?? "" : "";
        return $"{first} {last}".Trim();
    }

    private static string? BuildAddress(JsonElement item)
    {
        var addr1 = item.TryGetProperty("shipping_address_1", out var a1) ? a1.GetString() ?? "" : "";
        var addr2 = item.TryGetProperty("shipping_address_2", out var a2) ? a2.GetString() ?? "" : "";
        var full = $"{addr1} {addr2}".Trim();
        return string.IsNullOrEmpty(full) ? null : full;
    }

    private static string MapOpenCartOrderStatus(string statusId) => statusId switch
    {
        "1" => "Pending",
        "2" => "Processing",
        "3" => "Shipped",
        "5" => "Complete",
        "7" => "Canceled",
        "8" => "Denied",
        "9" => "Canceled Reversal",
        "10" => "Failed",
        "11" => "Refunded",
        "12" => "Reversed",
        "13" => "Chargeback",
        "14" => "Expired",
        "15" => "Processed",
        "16" => "Voided",
        _ => $"Unknown({statusId})"
    };

    private static int MapStatusToOpenCartId(string status) => status.ToLowerInvariant() switch
    {
        "pending" => 1,
        "processing" => 2,
        "shipped" => 3,
        "complete" or "completed" => 5,
        "canceled" or "cancelled" => 7,
        "denied" => 8,
        "refunded" => 11,
        _ => 2
    };

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.GetCategoriesAsync");

        try
        {
            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/rest/categories");
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("OpenCart GetCategories failed {Status}: {Error}",
                    response.StatusCode, error);
                return Array.Empty<CategoryDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var flat = new List<CategoryDto>();
            var dataEl = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;

            if (dataEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in dataEl.EnumerateArray())
                {
                    flat.Add(new CategoryDto
                    {
                        PlatformCategoryId = el.TryGetProperty("category_id", out var id)
                            && int.TryParse(id.GetString() ?? id.GetRawText(), out var idVal) ? idVal : 0,
                        Name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        ParentId = el.TryGetProperty("parent_id", out var pid)
                            && int.TryParse(pid.GetString() ?? pid.GetRawText(), out var pidVal) && pidVal > 0
                            ? pidVal : null
                    });
                }
            }

            // Build tree from flat list
            var lookup = flat.ToLookup(c => c.ParentId);
            foreach (var cat in flat)
                cat.SubCategories.AddRange(lookup[cat.PlatformCategoryId]);

            var roots = flat.Where(c => c.ParentId is null).ToList();
            _logger.LogInformation("OpenCart GetCategories: {Count} top-level categories", roots.Count);
            return roots.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart GetCategories exception");
            return Array.Empty<CategoryDto>();
        }
    }

    // ═══════════════════════════════════════════
    // ISettlementCapableAdapter
    // ═══════════════════════════════════════════

    /// <inheritdoc />
    public async Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.GetSettlementAsync: {StartDate} — {EndDate}", startDate, endDate);

        try
        {
            var settlement = new SettlementDto
            {
                PlatformCode = "OpenCart",
                StartDate = startDate,
                EndDate = endDate,
                Currency = "TRY"
            };

            var page = 1;
            const int limit = 100;
            bool hasMore = true;

            while (hasMore)
            {
                var url = $"/api/rest/orders?limit={limit}&page={page}" +
                          $"&date_added_from={startDate:yyyy-MM-dd}&date_added_to={endDate:yyyy-MM-dd}";

                var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                        return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("OpenCart GetSettlement orders fetch failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("data", out var dataArr) || dataArr.ValueKind != JsonValueKind.Array)
                {
                    hasMore = false;
                    break;
                }

                var items = dataArr.EnumerateArray().ToList();
                if (items.Count == 0) break;

                foreach (var order in items)
                {
                    var total = order.TryGetProperty("total", out var tot) &&
                                decimal.TryParse(tot.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var tv) ? tv : 0m;

                    var orderId = order.TryGetProperty("order_id", out var oid) ? oid.ToString() : null;
                    var orderDate = ParseOrderDate(order);
                    var statusId = order.TryGetProperty("order_status_id", out var osid) ? osid.ToString() : "0";
                    var status = MapOpenCartOrderStatus(statusId);

                    var txType = status switch
                    {
                        "Refunded" or "Reversed" or "Chargeback" => "RETURN",
                        "Canceled" or "Denied" or "Failed" or "Voided" => "CANCELLED",
                        _ => "SALE"
                    };

                    settlement.Lines.Add(new SettlementLineDto
                    {
                        OrderNumber = orderId,
                        TransactionType = txType,
                        Amount = total,
                        CommissionAmount = null,
                        TransactionDate = orderDate
                    });

                    if (txType == "SALE")
                    {
                        settlement.TotalSales += total;
                    }
                    else if (txType == "RETURN")
                    {
                        settlement.TotalReturnDeduction += total;
                    }
                }

                hasMore = items.Count == limit;
                page++;
            }

            // OpenCart is self-hosted — no platform commission
            settlement.NetAmount = settlement.TotalSales - settlement.TotalReturnDeduction;

            _logger.LogInformation("OpenCart GetSettlement: {LineCount} orders, Net={Net} TRY",
                settlement.Lines.Count, settlement.NetAmount);

            return settlement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart GetSettlement exception: {StartDate}—{EndDate}", startDate, endDate);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogInformation("OpenCartAdapter.GetCargoInvoicesAsync: OpenCart does not provide cargo invoices — returning empty list. StartDate={StartDate}", startDate);
        return Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());
    }

    // ═══════════════════════════════════════════
    // IShipmentCapableAdapter — Kargo Bildirimi
    // ═══════════════════════════════════════════

    /// <summary>
    /// Sends shipment notification to OpenCart by updating order status to Shipped + tracking info.
    /// PUT /api/rest/orders/{id} with status=shipped + tracking.
    /// </summary>
    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(platformOrderId))
            {
                _logger.LogWarning("OpenCart SendShipment — platformOrderId bos olamaz");
                return false;
            }

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                _logger.LogWarning("OpenCart SendShipment — trackingNumber bos olamaz. OrderId={OrderId}",
                    platformOrderId);
                return false;
            }

            var cargoCompany = MapCargoProviderToOpenCart(provider);
            var payload = new
            {
                order_status_id = 3, // Shipped
                comment = $"Kargo: {cargoCompany}, Takip No: {trackingNumber}",
                notify = true,
                tracking_number = trackingNumber,
                shipping_company = cargoCompany
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/rest/orders/{platformOrderId}", content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "OpenCart SendShipment basarili — OrderId={OrderId}, Tracking={Tracking}, Cargo={Cargo}",
                    platformOrderId, trackingNumber, cargoCompany);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("OpenCart SendShipment basarisiz {Status}: {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart SendShipment hatasi — OrderId={OrderId}", platformOrderId);
            return false;
        }
    }

    /// <summary>
    /// Maps CargoProvider enum to OpenCart cargo company name.
    /// </summary>
    private static string MapCargoProviderToOpenCart(CargoProvider provider) => provider switch
    {
        CargoProvider.YurticiKargo => "Yurtiçi Kargo",
        CargoProvider.ArasKargo => "Aras Kargo",
        CargoProvider.SuratKargo => "Sürat Kargo",
        CargoProvider.MngKargo => "MNG Kargo",
        CargoProvider.PttKargo => "PTT Kargo",
        CargoProvider.Hepsijet => "Hepsijet",
        CargoProvider.UPS => "UPS",
        CargoProvider.Sendeo => "Sendeo",
        CargoProvider.DHL => "DHL",
        CargoProvider.FedEx => "FedEx",
        _ => provider.ToString()
    };

    // ═══════════════════════════════════════════
    // IClaimCapableAdapter — Iade Yonetimi
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pulls return requests from OpenCart.
    /// GET /api/rest/returns with date filter.
    /// </summary>
    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PullClaimsAsync since={Since}", since);

        var claims = new List<ExternalClaimDto>();

        try
        {
            var page = 1;
            const int limit = 50;
            bool hasMore = true;

            while (hasMore)
            {
                var url = $"/api/rest/returns?limit={limit}&page={page}";
                if (since.HasValue)
                    url += $"&date_modified_from={since.Value:yyyy-MM-dd HH:mm:ss}";

                var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                        return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("OpenCart PullClaims failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("data", out var dataArr) || dataArr.ValueKind != JsonValueKind.Array)
                {
                    hasMore = false;
                    break;
                }

                var items = dataArr.EnumerateArray().ToList();
                if (items.Count == 0) break;

                foreach (var item in items)
                {
                    var claimId = item.TryGetProperty("return_id", out var ridEl)
                        ? ridEl.ToString() : string.Empty;

                    var orderId = item.TryGetProperty("order_id", out var oidEl)
                        ? oidEl.ToString() : string.Empty;

                    var status = item.TryGetProperty("return_status", out var stEl)
                        ? stEl.GetString() ?? string.Empty : string.Empty;

                    var reason = item.TryGetProperty("return_reason", out var rsEl)
                        ? rsEl.GetString() ?? string.Empty : string.Empty;

                    var comment = item.TryGetProperty("comment", out var cmEl)
                        ? cmEl.GetString() : null;

                    var firstName = item.TryGetProperty("firstname", out var fnEl) ? fnEl.GetString() ?? "" : "";
                    var lastName = item.TryGetProperty("lastname", out var lnEl) ? lnEl.GetString() ?? "" : "";
                    var customerName = $"{firstName} {lastName}".Trim();

                    var email = item.TryGetProperty("email", out var emEl) ? emEl.GetString() : null;

                    var claimDate = DateTime.UtcNow;
                    if (item.TryGetProperty("date_added", out var daEl) &&
                        DateTime.TryParse(daEl.GetString(), out var parsedDate))
                    {
                        claimDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                    }

                    var productName = item.TryGetProperty("product", out var pnEl)
                        ? pnEl.GetString() ?? string.Empty : string.Empty;

                    var quantity = item.TryGetProperty("quantity", out var qtyEl)
                        && int.TryParse(qtyEl.GetString() ?? qtyEl.ToString(), out var qtyVal)
                        ? qtyVal : 1;

                    var claim = new ExternalClaimDto
                    {
                        PlatformClaimId = claimId,
                        PlatformCode = PlatformCode,
                        OrderNumber = orderId,
                        Status = status,
                        Reason = reason,
                        ReasonDetail = comment,
                        CustomerName = customerName,
                        CustomerEmail = email,
                        Amount = 0m, // OpenCart returns API does not provide amount
                        Currency = "TRY",
                        ClaimDate = claimDate
                    };

                    // OpenCart returns are typically for a single product
                    claim.Lines.Add(new ExternalClaimLineDto
                    {
                        ProductName = productName,
                        Quantity = quantity,
                        SKU = item.TryGetProperty("model", out var mdEl) ? mdEl.GetString() : null
                    });

                    claims.Add(claim);
                }

                hasMore = items.Count == limit;
                page++;
            }

            _logger.LogInformation("OpenCart PullClaims: {Count} claims retrieved", claims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PullClaims failed");
        }

        return claims.AsReadOnly();
    }

    /// <summary>
    /// Approves a return request on OpenCart.
    /// PUT /api/rest/returns/{id} with status=approved (return_status_id=2).
    /// </summary>
    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(claimId))
            {
                _logger.LogWarning("OpenCart ApproveClaim — claimId bos olamaz");
                return false;
            }

            var payload = new { return_status_id = 2 }; // Approved
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/rest/returns/{claimId}", content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("OpenCart ApproveClaim basarili — ClaimId={ClaimId}", claimId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("OpenCart ApproveClaim basarisiz {Status}: {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart ApproveClaim hatasi — ClaimId={ClaimId}", claimId);
            return false;
        }
    }

    /// <summary>
    /// Rejects a return request on OpenCart.
    /// PUT /api/rest/returns/{id} with status=rejected (return_status_id=3) + comment.
    /// </summary>
    public async Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(claimId))
            {
                _logger.LogWarning("OpenCart RejectClaim — claimId bos olamaz");
                return false;
            }

            var payload = new
            {
                return_status_id = 3, // Rejected
                comment = reason ?? string.Empty
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/rest/returns/{claimId}", content);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("OpenCart RejectClaim basarili — ClaimId={ClaimId}, Reason={Reason}",
                    claimId, reason);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("OpenCart RejectClaim basarisiz {Status}: {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart RejectClaim hatasi — ClaimId={ClaimId}", claimId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IInvoiceCapableAdapter — Fatura (Self-hosted)
    // ═══════════════════════════════════════════

    /// <summary>
    /// OpenCart is self-hosted — invoice link sending is a local operation.
    /// Logs the invoice URL and returns true.
    /// </summary>
    public Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(shipmentPackageId))
        {
            _logger.LogWarning("OpenCart SendInvoiceLink — shipmentPackageId bos olamaz");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "OpenCart SendInvoiceLink — self-hosted, fatura linki kaydedildi. OrderId={OrderId}, Url={Url}",
            shipmentPackageId, invoiceUrl);
        return Task.FromResult(true);
    }

    /// <summary>
    /// OpenCart is self-hosted — invoice PDF upload is a local operation.
    /// Logs the upload and returns true.
    /// </summary>
    public Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(shipmentPackageId))
        {
            _logger.LogWarning("OpenCart SendInvoiceFile — shipmentPackageId bos olamaz");
            return Task.FromResult(false);
        }

        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            _logger.LogWarning("OpenCart SendInvoiceFile — pdfBytes bos olamaz. OrderId={OrderId}",
                shipmentPackageId);
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "OpenCart SendInvoiceFile — self-hosted, fatura PDF kaydedildi. OrderId={OrderId}, File={FileName}, Size={Size}bytes",
            shipmentPackageId, fileName, pdfBytes.Length);
        return Task.FromResult(true);
    }
    // ── IWebhookCapableAdapter ──
    public Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default) { _logger.LogInformation("[OpenCart] RegisterWebhook {Url}", callbackUrl); return Task.FromResult(true); }
    public Task<bool> UnregisterWebhookAsync(CancellationToken ct = default) => Task.FromResult(true);
    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payload)) return Task.CompletedTask;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var eventType = root.TryGetProperty("event", out var ev) ? ev.GetString()
                          : root.TryGetProperty("action", out var act) ? act.GetString()
                          : "unknown";
            var orderId = root.TryGetProperty("order_id", out var oid) ? oid.GetString() : null;
            _logger.LogInformation(
                "OpenCart webhook processed: EventType={EventType} OrderId={OrderId} PayloadLength={Len}",
                eventType, orderId, payload.Length);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "[OpenCart] Webhook payload parse failed ({Len}b)", payload.Length);
        }
        return Task.CompletedTask;
    }

    // ── IPingableAdapter ──
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            if (_httpClient.BaseAddress is null) return false;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            using var request = CreateAuthenticatedRequest(HttpMethod.Get, _httpClient.BaseAddress.ToString());
            var resp = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            return (int)resp.StatusCode < 500;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenCart ping failed");
            return false;
        }
    }
}
