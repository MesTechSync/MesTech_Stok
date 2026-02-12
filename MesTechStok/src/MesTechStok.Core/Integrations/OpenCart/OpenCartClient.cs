using System;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MesTechStok.Core.Integrations.OpenCart.Dtos;

namespace MesTechStok.Core.Integrations.OpenCart
{
    public class OpenCartClient : IOpenCartClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Telemetry.IResilienceTelemetry _telemetry; // injected
        private readonly string _baseUrl;
        private readonly string _apiToken;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;
        private bool _isConnected = false;
        private static readonly object _syncStateFileLock = new();
        private readonly string _syncStateFilePath;
        private ConcurrentDictionary<string, DateTime> _syncStateCache = new();

        // Interface Events
        public event EventHandler<ApiCallSuccessEventArgs>? ApiCallSuccess;
        public event EventHandler<ApiCallErrorEventArgs>? ApiCallError;
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

        // Interface Properties
        public bool IsConnected => _isConnected;

        public OpenCartClient(string baseUrl, string apiToken)
            : this(baseUrl, apiToken, Telemetry.NoopResilienceTelemetry.Instance, new MesTechStok.Core.Integrations.OpenCart.Http.RetryAndCorrelationHandler()) { }

        public OpenCartClient(string baseUrl, string apiToken, Telemetry.IResilienceTelemetry telemetry, HttpMessageHandler handler)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _apiToken = apiToken ?? throw new ArgumentNullException(nameof(apiToken));
            _httpClient = new HttpClient(handler ?? throw new ArgumentNullException(nameof(handler)));
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MesTechStok/1.0");
            _telemetry = telemetry ?? Telemetry.NoopResilienceTelemetry.Instance;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            // Sync state persistence (AppData/MesTechStok/sync-state.json)
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "MesTechStok");
            Directory.CreateDirectory(appFolder);
            _syncStateFilePath = Path.Combine(appFolder, "sync-state.json");
            LoadSyncState();
        }

        public async Task<bool> ConnectAsync(string apiUrl, string apiKey)
        {
            try
            {
                // Update connection details
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Test connection
                var isConnected = await TestConnectionAsync();
                _isConnected = isConnected;

                OnConnectionStatusChanged(new ConnectionStatusEventArgs
                {
                    IsConnected = isConnected,
                    Message = isConnected ? "Connected successfully" : "Connection failed"
                });

                return isConnected;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "CONNECT",
                    Endpoint = apiUrl,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                return false;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            OnConnectionStatusChanged(new ConnectionStatusEventArgs
            {
                IsConnected = false,
                Message = "Disconnected"
            });
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/system/info");
                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    OnApiCallSuccess(new ApiCallSuccessEventArgs
                    {
                        Method = "GET",
                        Endpoint = "/api/system/info",
                        Duration = duration
                    });
                    _telemetry.OnApiCall("/api/system/info", "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return true;
                }
                else
                {
                    OnApiCallError(new ApiCallErrorEventArgs
                    {
                        Method = "GET",
                        Endpoint = "/api/system/info",
                        ErrorMessage = $"HTTP {response.StatusCode}",
                        StatusCode = (int)response.StatusCode
                    });
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    _telemetry.OnApiCall("/api/system/info", "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "GET",
                    Endpoint = "/api/system/info",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _telemetry.OnApiCall("/api/system/info", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                return false;
            }
        }

        // Interface Methods
        public async Task<IEnumerable<OpenCartProduct>> GetAllProductsAsync()
        {
            return await GetProductsAsync();
        }

        public async Task<OpenCartProduct?> GetProductByIdAsync(int productId)
        {
            return await GetProductAsync(productId);
        }

        public async Task<int?> CreateProductAsync(OpenCartProduct product)
        {
            var created = await CreateProductInternalAsync(product);
            return created?.ProductId;
        }

        public async Task<bool> UpdateProductAsync(int productId, OpenCartProduct product)
        {
            var updated = await UpdateProductInternalAsync(productId, product);
            return updated != null;
        }

        public async Task<IEnumerable<OpenCartOrder>> GetAllOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await GetOrdersAsync();
        }

        public async Task<OpenCartOrder?> GetOrderByIdAsync(int orderId)
        {
            return await GetOrderAsync(orderId);
        }

        public async Task<IEnumerable<OpenCartOrder>> GetNewOrdersAsync(DateTime fromDate)
        {
            return await GetOrdersByDateAsync(fromDate);
        }

        public async Task<int?> CreateCategoryAsync(OpenCartCategory category)
        {
            var created = await CreateCategoryInternalAsync(category);
            return created?.CategoryId;
        }

        public async Task<OpenCartCustomer?> GetCustomerByIdAsync(int customerId)
        {
            return await GetCustomerAsync(customerId);
        }

        public async Task<OpenCartCustomer?> GetCustomerByEmailAsync(string email)
        {
            return await GetCustomerByEmailInternalAsync(email);
        }

        public async Task<DateTime?> GetLastSyncDateAsync(string syncType)
        {
            return await GetLastSyncDateInternalAsync(syncType);
        }

        public async Task<bool> UpdateLastSyncDateAsync(string syncType, DateTime syncDate)
        {
            return await UpdateLastSyncDateInternalAsync(syncType, syncDate);
        }

        public async Task<OpenCartSyncResult> BulkSyncProductsAsync(IEnumerable<OpenCartProduct> products)
        {
            return await BulkSyncProductsInternalAsync(products);
        }

        public async Task<OpenCartSyncResult> BulkUpdateStockAsync(IEnumerable<OpenCartStockUpdate> stockUpdates)
        {
            return await BulkUpdateStockInternalAsync(stockUpdates);
        }

        // Legacy/Internal Methods
        public async Task<IEnumerable<OpenCartProduct>> GetProductsAsync(int page = 1, int limit = 100)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/products?page={page}&limit={limit}");
                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenCartApiResponse<List<OpenCartProduct>>>(content, _jsonOptions);

                OnApiCallSuccess(new ApiCallSuccessEventArgs
                {
                    Method = "GET",
                    Endpoint = "/api/products",
                    Duration = duration
                });
                _telemetry.OnApiCall("/api/products", "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                return result?.Data ?? new List<OpenCartProduct>();
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "GET",
                    Endpoint = "/api/products",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _telemetry.OnApiCall("/api/products", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get products: {ex.Message}", ex);
            }
        }

        public async Task<OpenCartProduct> GetProductAsync(int productId)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/products/{productId}");
                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartProduct>>(content, _jsonOptions);
                    OnApiCallSuccess(new ApiCallSuccessEventArgs { Method = "GET", Endpoint = $"/api/products/{productId}", Duration = duration });
                    _telemetry.OnApiCall($"/api/products/{productId}", "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = $"/api/products/{productId}", ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall($"/api/products/{productId}", "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get product {productId}: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "GET",
                    Endpoint = $"/api/products/{productId}",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _telemetry.OnApiCall($"/api/products/{productId}", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<OpenCartProduct?> GetProductBySkuAsync(string sku)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/products/sku/{sku}");
                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartProduct>>(content, _jsonOptions);
                    OnApiCallSuccess(new ApiCallSuccessEventArgs { Method = "GET", Endpoint = $"/api/products/sku/{sku}", Duration = duration });
                    _telemetry.OnApiCall($"/api/products/sku/{sku}", "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = $"/api/products/sku/{sku}", ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall($"/api/products/sku/{sku}", "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "GET",
                    Endpoint = $"/api/products/sku/{sku}",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _telemetry.OnApiCall($"/api/products/sku/{sku}", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                return null;
            }
        }

        private async Task<OpenCartProduct?> CreateProductInternalAsync(OpenCartProduct product)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(product, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/products") { Content = content };
                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartProduct>>(responseContent, _jsonOptions);

                OnApiCallSuccess(new ApiCallSuccessEventArgs
                {
                    Method = "POST",
                    Endpoint = "/api/products",
                    Duration = duration
                });
                _telemetry.OnApiCall("/api/products", "POST", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                return result?.Data ?? null;
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "POST",
                    Endpoint = "/api/products",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _telemetry.OnApiCall("/api/products", "POST", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to create product: {ex.Message}", ex);
            }
        }

        private async Task<OpenCartProduct?> UpdateProductInternalAsync(int productId, OpenCartProduct product)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(product, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/api/products/{productId}") { Content = content };
                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartProduct>>(responseContent, _jsonOptions);
                    OnApiCallSuccess(new ApiCallSuccessEventArgs { Method = "PUT", Endpoint = $"/api/products/{productId}", Duration = duration });
                    _telemetry.OnApiCall($"/api/products/{productId}", "PUT", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data ?? null;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "PUT", Endpoint = $"/api/products/{productId}", ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall($"/api/products/{productId}", "PUT", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to update product {productId}: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "PUT",
                    Endpoint = $"/api/products/{productId}",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _telemetry.OnApiCall($"/api/products/{productId}", "PUT", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to update product {productId}: {ex.Message}", ex);
            }
        }

        // Additional implementation methods would continue here...
        // For brevity, I'll add placeholder implementations for the remaining interface methods

        private async Task<IEnumerable<OpenCartOrder>> GetOrdersByDateAsync(DateTime fromDate)
        {
            // Implementation for getting orders by date
            return await GetOrdersAsync();
        }

        private async Task<OpenCartCategory?> CreateCategoryInternalAsync(OpenCartCategory category)
        {
            // Implementation for creating category
            try
            {
                var json = JsonSerializer.Serialize(category, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/categories", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartCategory>>(responseContent, _jsonOptions);

                return result?.Data ?? null;
            }
            catch (Exception ex)
            {
                throw new OpenCartApiException($"Failed to create category: {ex.Message}", ex);
            }
        }

        private async Task<OpenCartCustomer?> GetCustomerByEmailInternalAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email)) return null;
                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/customers/email/{Uri.EscapeDataString(email)}";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (!response.IsSuccessStatusCode)
                {
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = response.StatusCode.ToString(), StatusCode = (int)response.StatusCode });
                    var catFail = Telemetry.ErrorCategoryMapper.From(response, null);
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, catFail, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return null;
                }
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartCustomer>>(content, _jsonOptions);
                OnApiCallSuccess(new ApiCallSuccessEventArgs { Method = "GET", Endpoint = endpoint, Duration = duration });
                _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                return result?.Data;
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = $"/api/customers/email/{{email}}", ErrorMessage = ex.Message, Exception = ex });
                _telemetry.OnApiCall($"/api/customers/email/{email}", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                return null;
            }
        }

        private async Task<DateTime?> GetLastSyncDateInternalAsync(string syncType)
        {
            if (string.IsNullOrWhiteSpace(syncType)) return null;
            if (_syncStateCache.TryGetValue(syncType, out var dt)) return dt;
            // attempt to reload (if external process updated file)
            LoadSyncState();
            return _syncStateCache.TryGetValue(syncType, out dt) ? dt : null;
        }

        private async Task<bool> UpdateLastSyncDateInternalAsync(string syncType, DateTime syncDate)
        {
            if (string.IsNullOrWhiteSpace(syncType)) return false;
            _syncStateCache[syncType] = syncDate;
            PersistSyncState();
            return true;
        }

        private async Task<OpenCartSyncResult> BulkSyncProductsInternalAsync(IEnumerable<OpenCartProduct> products)
        {
            var result = new OpenCartSyncResult { SyncDate = DateTime.UtcNow };
            if (products == null) return result;
            foreach (var p in products)
            {
                try
                {
                    OpenCartProduct? existing = null;
                    if (!string.IsNullOrWhiteSpace(p?.Model))
                    {
                        existing = await GetProductBySkuAsync(p.Model);
                    }
                    if (existing == null && p.ProductId > 0)
                    {
                        existing = await GetProductAsync(p.ProductId);
                    }
                    if (existing == null)
                    {
                        var createdId = await CreateProductAsync(p);
                        if (createdId.HasValue)
                        {
                            result.SuccessCount++; result.TotalProcessed++;
                        }
                        else
                        {
                            result.ErrorCount++; result.TotalProcessed++; result.Errors.Add($"Create failed for product {p?.Model ?? p?.ProductId.ToString()} ");
                        }
                    }
                    else
                    {
                        var ok = await UpdateProductAsync(existing.ProductId, p);
                        if (ok) { result.SuccessCount++; } else { result.ErrorCount++; result.Errors.Add($"Update failed {existing.ProductId}"); }
                        result.TotalProcessed++;
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++; result.TotalProcessed++; result.Errors.Add(ex.Message);
                }
            }
            result.IsSuccess = result.ErrorCount == 0;
            result.EndTime = DateTime.UtcNow;
            return result;
        }

        private async Task<OpenCartSyncResult> BulkUpdateStockInternalAsync(IEnumerable<OpenCartStockUpdate> stockUpdates)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(stockUpdates, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/products/bulk-stock-update", content);
                var duration = DateTime.UtcNow - startTime;

                var isSuccess = response.IsSuccessStatusCode;

                if (isSuccess)
                {
                    OnApiCallSuccess(new ApiCallSuccessEventArgs
                    {
                        Method = "POST",
                        Endpoint = "/api/products/bulk-stock-update",
                        Duration = duration
                    });
                    _telemetry.OnApiCall("/api/products/bulk-stock-update", "POST", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                }
                else
                {
                    OnApiCallError(new ApiCallErrorEventArgs
                    {
                        Method = "POST",
                        Endpoint = "/api/products/bulk-stock-update",
                        ErrorMessage = $"HTTP {response.StatusCode}",
                        StatusCode = (int)response.StatusCode
                    });
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    _telemetry.OnApiCall("/api/products/bulk-stock-update", "POST", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                }

                return new OpenCartSyncResult
                {
                    IsSuccess = isSuccess,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "POST",
                    Endpoint = "/api/products/bulk-stock-update",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _telemetry.OnApiCall("/api/products/bulk-stock-update", "POST", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                return new OpenCartSyncResult
                {
                    IsSuccess = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // Missing Interface Methods
        public async Task<bool> UpdateProductStockAsync(int productId, int quantity)
        {
            try
            {
                var stockUpdate = new { quantity = quantity };
                var json = JsonSerializer.Serialize(stockUpdate, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"{_baseUrl}/api/products/{productId}/stock", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "PATCH",
                    Endpoint = $"/api/products/{productId}/stock",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                return false;
            }
        }

        public async Task<bool> UpdateProductPriceAsync(int productId, decimal price)
        {
            try
            {
                var priceUpdate = new { price = price };
                var json = JsonSerializer.Serialize(priceUpdate, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"{_baseUrl}/api/products/{productId}/price", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "PATCH",
                    Endpoint = $"/api/products/{productId}/price",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/products/{productId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                OnApiCallError(new ApiCallErrorEventArgs
                {
                    Method = "DELETE",
                    Endpoint = $"/api/products/{productId}",
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                return false;
            }
        }

        // Event handlers
        protected virtual void OnApiCallSuccess(ApiCallSuccessEventArgs e)
        {
            ApiCallSuccess?.Invoke(this, e);
        }

        protected virtual void OnApiCallError(ApiCallErrorEventArgs e)
        {
            ApiCallError?.Invoke(this, e);
        }

        protected virtual void OnConnectionStatusChanged(ConnectionStatusEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, e);
        }

        public async Task<IEnumerable<OpenCartOrder>> GetOrdersAsync(int page = 1, int limit = 100)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/orders?page={page}&limit={limit}";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<List<OpenCartOrder>>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data ?? new List<OpenCartOrder>();
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get orders: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall("/api/orders", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get orders: {ex.Message}", ex);
            }
        }

        public async Task<OpenCartOrder> GetOrderAsync(int orderId)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/orders/{orderId}";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartOrder>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get order {orderId}: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall($"/api/orders/{orderId}", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<OpenCartOrder>> GetOrdersByStatusAsync(string status)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/orders?status={status}";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<List<OpenCartOrder>>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data ?? new List<OpenCartOrder>();
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get orders by status {status}: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall("/api/orders", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get orders by status {status}: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                var statusUpdate = new { order_status = status };
                var json = JsonSerializer.Serialize(statusUpdate, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/orders/{orderId}/status";
                var response = await _httpClient.PatchAsync($"{_baseUrl}{endpoint}", content);
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    _telemetry.OnApiCall(endpoint, "PATCH", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return true;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "PATCH", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "PATCH", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall($"/api/orders/{orderId}/status", "PATCH", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to update order {orderId} status: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<OpenCartCategory>> GetCategoriesAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = "/api/categories";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<List<OpenCartCategory>>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data ?? new List<OpenCartCategory>();
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get categories: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall("/api/categories", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get categories: {ex.Message}", ex);
            }
        }



        public async Task<IEnumerable<OpenCartCustomer>> GetCustomersAsync(int page = 1, int limit = 100)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/customers?page={page}&limit={limit}";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<List<OpenCartCustomer>>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data ?? new List<OpenCartCustomer>();
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get customers: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall("/api/customers", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get customers: {ex.Message}", ex);
            }
        }

        public async Task<OpenCartCustomer> GetCustomerAsync(int customerId)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/customers/{customerId}";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartCustomer>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get customer {customerId}: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall($"/api/customers/{customerId}", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<OpenCartSyncReport> GetSyncReportAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var from = fromDate.ToString("yyyy-MM-dd");
                var to = toDate.ToString("yyyy-MM-dd");
                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/sync/report?from={from}&to={to}";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartSyncReport>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data ?? new OpenCartSyncReport();
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get sync report: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall("/api/sync/report", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get sync report: {ex.Message}", ex);
            }
        }



        public async Task<bool> BulkUpdatePricesAsync(IEnumerable<OpenCartPriceUpdate> priceUpdates)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(priceUpdates, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var endpoint = "/api/products/bulk-price-update";
                var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    _telemetry.OnApiCall(endpoint, "POST", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return true;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "POST", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "POST", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall("/api/products/bulk-price-update", "POST", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to bulk update prices: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<OpenCartInventoryItem>> GetInventoryAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = "/api/inventory";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<List<OpenCartInventoryItem>>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data ?? new List<OpenCartInventoryItem>();
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get inventory: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall("/api/inventory", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get inventory: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateInventoryAsync(int productId, OpenCartInventoryUpdate inventoryUpdate)
        {
            try
            {
                var json = JsonSerializer.Serialize(inventoryUpdate, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var startTime = DateTime.UtcNow;
                var endpoint = $"/api/inventory/{productId}";
                var response = await _httpClient.PutAsync($"{_baseUrl}{endpoint}", content);
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    _telemetry.OnApiCall(endpoint, "PUT", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return true;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "PUT", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "PUT", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall($"/api/inventory/{productId}", "PUT", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to update inventory for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<OpenCartSystemInfo> GetSystemInfoAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = "/api/system/info";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartSystemInfo>>(content, _jsonOptions);
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return result?.Data ?? new OpenCartSystemInfo();
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    OnApiCallError(new ApiCallErrorEventArgs { Method = "GET", Endpoint = endpoint, ErrorMessage = $"HTTP {response.StatusCode}", StatusCode = (int)response.StatusCode });
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    throw new OpenCartApiException($"Failed to get system info: HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _telemetry.OnApiCall("/api/system/info", "GET", TimeSpan.Zero, false, null, Telemetry.ErrorCategoryMapper.From(null, ex), MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                throw new OpenCartApiException($"Failed to get system info: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidateApiKeyAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var endpoint = "/api/validate";
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var duration = DateTime.UtcNow - startTime;
                if (response.IsSuccessStatusCode)
                {
                    _telemetry.OnApiCall(endpoint, "GET", duration, true, (int)response.StatusCode, Telemetry.OpenCartErrorCategory.None, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return true;
                }
                else
                {
                    var cat = Telemetry.ErrorCategoryMapper.From(response, null);
                    _telemetry.OnApiCall(endpoint, "GET", duration, false, (int)response.StatusCode, cat, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                    return false;
                }
            }
            catch
            {
                _telemetry.OnApiCall("/api/validate", "GET", TimeSpan.Zero, false, null, Telemetry.OpenCartErrorCategory.Network, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        #region SyncState Persistence
        private void LoadSyncState()
        {
            try
            {
                if (File.Exists(_syncStateFilePath))
                {
                    var json = File.ReadAllText(_syncStateFilePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
                    if (data != null)
                    {
                        foreach (var kv in data)
                        {
                            if (DateTime.TryParse(kv.Value, out var dt))
                                _syncStateCache[kv.Key] = dt;
                        }
                    }
                }
            }
            catch { /* swallow - non critical */ }
        }

        private void PersistSyncState()
        {
            try
            {
                lock (_syncStateFileLock)
                {
                    var dict = _syncStateCache.ToDictionary(k => k.Key, v => v.Value.ToString("o"));
                    var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_syncStateFilePath, json);
                }
            }
            catch { /* swallow - non critical */ }
        }
        #endregion
    }

    // Helper classes for API responses
    public class OpenCartApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class OpenCartApiException : Exception
    {
        public OpenCartApiException(string message) : base(message) { }
        public OpenCartApiException(string message, Exception innerException) : base(message, innerException) { }
    }
}
