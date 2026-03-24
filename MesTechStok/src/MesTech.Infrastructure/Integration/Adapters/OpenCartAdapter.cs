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
/// OpenCart platform adaptoru — IIntegratorAdapter + IOrderCapableAdapter + ICustomerSyncCapable + ICategorySyncCapable.
/// Polly retry pipeline, batch stok guncelleme, siparis cekme, musteri ve kategori senkronizasyonu destegi.
/// OpenCart'ta kargo yonetimi yok (SupportsShipment = false).
/// </summary>
public class OpenCartAdapter : IIntegratorAdapter, IOrderCapableAdapter,
    ICustomerSyncCapable, ICategorySyncCapable
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
    public bool SupportsShipment => false;

    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        credentials.TryGetValue("ApiToken", out var apiToken);
        credentials.TryGetValue("BaseUrl", out var baseUrl);

        _apiToken = apiToken ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(baseUrl))
            _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Oc-Restadmin-Id", _apiToken);
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
                async token => await _httpClient.GetAsync(
                    new Uri("/api/rest/products?limit=1", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri("/api/rest/products", UriKind.Relative), content, token).ConfigureAwait(false);
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
                    async token => await _httpClient.GetAsync(
                        new Uri($"/api/rest/products?limit={limit}&page={page}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PutAsync(
                        new Uri($"/api/rest/products/{productId}", UriKind.Relative), content, token).ConfigureAwait(false);
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
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PutAsync(
                        new Uri($"/api/rest/products/{productId}", UriKind.Relative), content, token).ConfigureAwait(false);
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
                    async token => await _httpClient.GetAsync(
                        new Uri(url, UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PutAsync(
                        new Uri($"/api/rest/orders/{packageId}", UriKind.Relative), content, token).ConfigureAwait(false);
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
                    async token => await _httpClient.GetAsync(
                        new Uri(url, UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return isUpdate
                        ? await _httpClient.PutAsync(
                            new Uri($"/api/rest/customers/{customer.Id}", UriKind.Relative), content, token).ConfigureAwait(false)
                        : await _httpClient.PostAsync(
                            new Uri("/api/rest/customers", UriKind.Relative), content, token).ConfigureAwait(false);
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
                async token => await _httpClient.GetAsync(
                    new Uri("/api/rest/categories", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return isUpdate
                        ? await _httpClient.PutAsync(
                            new Uri($"/api/rest/categories/{category.Id}", UriKind.Relative), content, token).ConfigureAwait(false)
                        : await _httpClient.PostAsync(
                            new Uri("/api/rest/categories", UriKind.Relative), content, token).ConfigureAwait(false);
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
                async token => await _httpClient.GetAsync(
                    new Uri("/api/rest/categories", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
}
