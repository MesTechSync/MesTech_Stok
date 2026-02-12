using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Integrations.OpenCart
{
    /// <summary>
    /// Gerçek OpenCart REST API implementasyonu
    /// OpenCart REST API v3+ için optimize edilmiş
    /// </summary>
    // NOTE: This client is not currently used; kept as a reference. Disable from build to avoid type conflicts.
    /* public class RealOpenCartApiClient : IOpenCartClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RealOpenCartApiClient> _logger;
        private readonly OpenCartApiSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        public RealOpenCartApiClient(
            HttpClient httpClient,
            ILogger<RealOpenCartApiClient> logger,
            IOptions<OpenCartApiSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-Oc-Restadmin-Id", _settings.ApiUsername);
            _httpClient.DefaultRequestHeaders.Add("X-Oc-Merchant-Id", _settings.StoreId);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Tüm ürünleri getirir
        /// </summary>
        public async Task<IEnumerable<Product>> GetProductsAsync(int page = 1, int limit = 100, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();
            
            try
            {
                var requestUrl = $"api/products?page={page}&limit={limit}&key={_settings.ApiKey}";
                
                _logger.LogDebug("[OpenCartAPI] Fetching products: page={Page}, limit={Limit}, CorrelationId={CorrelationId}",
                    page, limit, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenCartApiResponse<List<OpenCartProduct>>>(jsonContent, _jsonOptions);

                if (apiResponse?.Success != true || apiResponse.Data == null)
                {
                    _logger.LogWarning("[OpenCartAPI] API returned unsuccessful response or null data");
                    return new List<Product>();
                }

                var products = apiResponse.Data.Select(MapToProduct).ToList();
                
                _logger.LogInformation("[OpenCartAPI] Successfully fetched {Count} products from OpenCart",
                    products.Count);

                return products;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] HTTP error while fetching products");
                throw new OpenCartApiException("Network error occurred while fetching products", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] Request timeout while fetching products");
                throw new OpenCartApiException("Request timeout while fetching products", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] JSON parsing error while fetching products");
                throw new OpenCartApiException("Invalid JSON response from OpenCart API", ex);
            }
        }

        /// <summary>
        /// Belirli bir ürünü getirir
        /// </summary>
        public async Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            try
            {
                var requestUrl = $"api/products/{productId}?key={_settings.ApiKey}";
                
                _logger.LogDebug("[OpenCartAPI] Fetching product: {ProductId}, CorrelationId={CorrelationId}",
                    productId, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("[OpenCartAPI] Product not found: {ProductId}", productId);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartProduct>>(jsonContent, _jsonOptions);

                if (apiResponse?.Success != true || apiResponse.Data == null)
                {
                    _logger.LogWarning("[OpenCartAPI] Product not found or API error: {ProductId}", productId);
                    return null;
                }

                var product = MapToProduct(apiResponse.Data);
                
                _logger.LogDebug("[OpenCartAPI] Successfully fetched product: {ProductId}", productId);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] Error fetching product: {ProductId}", productId);
                throw new OpenCartApiException($"Error fetching product {productId}", ex);
            }
        }

        /// <summary>
        /// Ürün oluşturur
        /// </summary>
        public async Task<string> CreateProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            try
            {
                var openCartProduct = MapFromProduct(product);
                var jsonContent = JsonSerializer.Serialize(openCartProduct, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var requestUrl = $"api/products?key={_settings.ApiKey}";
                
                _logger.LogDebug("[OpenCartAPI] Creating product: {ProductName}, CorrelationId={CorrelationId}",
                    product.Name, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenCartApiResponse<OpenCartProductCreateResponse>>(responseContent, _jsonOptions);

                if (apiResponse?.Success != true || apiResponse.Data?.ProductId == null)
                {
                    throw new OpenCartApiException("Failed to create product - API returned unsuccessful response");
                }

                _logger.LogInformation("[OpenCartAPI] Successfully created product: {ProductName}, ID: {ProductId}",
                    product.Name, apiResponse.Data.ProductId);

                return apiResponse.Data.ProductId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] Error creating product: {ProductName}", product.Name);
                throw new OpenCartApiException($"Error creating product {product.Name}", ex);
            }
        }

        /// <summary>
        /// Ürün günceller
        /// </summary>
        public async Task<bool> UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            try
            {
                var openCartProduct = MapFromProduct(product);
                var jsonContent = JsonSerializer.Serialize(openCartProduct, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var requestUrl = $"api/products/{product.Id}?key={_settings.ApiKey}";
                
                _logger.LogDebug("[OpenCartAPI] Updating product: {ProductId}, CorrelationId={CorrelationId}",
                    product.Id, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.PutAsync(requestUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenCartApiResponse<object>>(responseContent, _jsonOptions);

                var success = apiResponse?.Success == true;
                
                if (success)
                {
                    _logger.LogInformation("[OpenCartAPI] Successfully updated product: {ProductId}", product.Id);
                }
                else
                {
                    _logger.LogWarning("[OpenCartAPI] Failed to update product: {ProductId}", product.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] Error updating product: {ProductId}", product.Id);
                throw new OpenCartApiException($"Error updating product {product.Id}", ex);
            }
        }

        /// <summary>
        /// Stok seviyesini günceller
        /// </summary>
        public async Task<bool> UpdateStockLevelAsync(string productId, int quantity, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            try
            {
                var stockUpdate = new { quantity = quantity };
                var jsonContent = JsonSerializer.Serialize(stockUpdate, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var requestUrl = $"api/products/{productId}/stock?key={_settings.ApiKey}";
                
                _logger.LogDebug("[OpenCartAPI] Updating stock: {ProductId}={Quantity}, CorrelationId={CorrelationId}",
                    productId, quantity, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.PatchAsync(requestUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenCartApiResponse<object>>(responseContent, _jsonOptions);

                var success = apiResponse?.Success == true;
                
                if (success)
                {
                    _logger.LogInformation("[OpenCartAPI] Successfully updated stock: {ProductId}={Quantity}", 
                        productId, quantity);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] Error updating stock: {ProductId}={Quantity}", 
                    productId, quantity);
                throw new OpenCartApiException($"Error updating stock for product {productId}", ex);
            }
        }

        /// <summary>
        /// Siparişleri getirir
        /// </summary>
        public async Task<IEnumerable<Order>> GetOrdersAsync(DateTime? since = null, int page = 1, int limit = 100, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            try
            {
                var requestUrl = $"api/orders?page={page}&limit={limit}&key={_settings.ApiKey}";
                
                if (since.HasValue)
                {
                    var sinceParam = since.Value.ToString("yyyy-MM-dd HH:mm:ss");
                    requestUrl += $"&date_added_from={Uri.EscapeDataString(sinceParam)}";
                }

                _logger.LogDebug("[OpenCartAPI] Fetching orders: since={Since}, page={Page}, CorrelationId={CorrelationId}",
                    since, page, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenCartApiResponse<List<OpenCartOrder>>>(jsonContent, _jsonOptions);

                if (apiResponse?.Success != true || apiResponse.Data == null)
                {
                    _logger.LogWarning("[OpenCartAPI] API returned unsuccessful response for orders");
                    return new List<Order>();
                }

                var orders = apiResponse.Data.Select(MapToOrder).ToList();
                
                _logger.LogInformation("[OpenCartAPI] Successfully fetched {Count} orders from OpenCart",
                    orders.Count);

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] Error fetching orders");
                throw new OpenCartApiException("Error fetching orders from OpenCart", ex);
            }
        }

        /// <summary>
        /// Sipariş durumunu günceller
        /// </summary>
        public async Task<bool> UpdateOrderStatusAsync(string orderId, OrderStatus status, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            try
            {
                var statusUpdate = new { order_status_id = MapOrderStatusToOpenCart(status) };
                var jsonContent = JsonSerializer.Serialize(statusUpdate, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var requestUrl = $"api/orders/{orderId}/status?key={_settings.ApiKey}";
                
                _logger.LogDebug("[OpenCartAPI] Updating order status: {OrderId}={Status}, CorrelationId={CorrelationId}",
                    orderId, status, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.PatchAsync(requestUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenCartApiResponse<object>>(responseContent, _jsonOptions);

                var success = apiResponse?.Success == true;
                
                if (success)
                {
                    _logger.LogInformation("[OpenCartAPI] Successfully updated order status: {OrderId}={Status}", 
                        orderId, status);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] Error updating order status: {OrderId}={Status}", 
                    orderId, status);
                throw new OpenCartApiException($"Error updating order status for {orderId}", ex);
            }
        }

        /// <summary>
        /// API bağlantı testi
        /// </summary>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var requestUrl = $"api/system/info?key={_settings.ApiKey}";
                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenCartAPI] Connection test failed");
                return false;
            }
        }

        #region Mapping Methods

        private Product MapToProduct(OpenCartProduct ocProduct)
        {
            return new Product
            {
                Id = ocProduct.ProductId?.ToString() ?? Guid.NewGuid().ToString(),
                Name = ocProduct.Name ?? string.Empty,
                Description = ocProduct.Description ?? string.Empty,
                Barcode = ocProduct.Ean ?? ocProduct.Upc ?? ocProduct.Sku ?? string.Empty,
                Price = decimal.TryParse(ocProduct.Price, out var price) ? price : 0,
                StockQuantity = int.TryParse(ocProduct.Quantity, out var qty) ? qty : 0,
                CategoryId = ocProduct.CategoryId?.ToString(),
                SKU = ocProduct.Sku ?? string.Empty,
                IsActive = ocProduct.Status == "1",
                CreatedAt = DateTime.TryParse(ocProduct.DateAdded, out var created) ? created : DateTime.UtcNow,
                LastModified = DateTime.TryParse(ocProduct.DateModified, out var modified) ? modified : DateTime.UtcNow
            };
        }

        private OpenCartProduct MapFromProduct(Product product)
        {
            return new OpenCartProduct
            {
                ProductId = int.TryParse(product.Id, out var id) ? id : null,
                Name = product.Name,
                Description = product.Description,
                Sku = product.SKU,
                Ean = product.Barcode,
                Price = product.Price.ToString("F2"),
                Quantity = product.StockQuantity.ToString(),
                Status = product.IsActive ? "1" : "0",
                CategoryId = int.TryParse(product.CategoryId, out var catId) ? catId : 0
            };
        }

        private Order MapToOrder(OpenCartOrder ocOrder)
        {
            return new Order
            {
                Id = ocOrder.OrderId?.ToString() ?? Guid.NewGuid().ToString(),
                OrderNumber = ocOrder.OrderId?.ToString() ?? string.Empty,
                CustomerId = ocOrder.CustomerId?.ToString(),
                TotalAmount = decimal.TryParse(ocOrder.Total, out var total) ? total : 0,
                Status = MapOrderStatusFromOpenCart(ocOrder.OrderStatusId),
                OrderDate = DateTime.TryParse(ocOrder.DateAdded, out var date) ? date : DateTime.UtcNow,
                LastModified = DateTime.TryParse(ocOrder.DateModified, out var modified) ? modified : DateTime.UtcNow,
                Items = ocOrder.OrderProducts?.Select(MapToOrderItem).ToList() ?? new List<OrderItem>()
            };
        }

        private OrderItem MapToOrderItem(OpenCartOrderProduct ocOrderProduct)
        {
            return new OrderItem
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = ocOrderProduct.ProductId?.ToString() ?? string.Empty,
                ProductName = ocOrderProduct.Name ?? string.Empty,
                Quantity = int.TryParse(ocOrderProduct.Quantity, out var qty) ? qty : 0,
                UnitPrice = decimal.TryParse(ocOrderProduct.Price, out var price) ? price : 0,
                TotalPrice = decimal.TryParse(ocOrderProduct.Total, out var total) ? total : 0
            };
        }

        private OrderStatus MapOrderStatusFromOpenCart(int? statusId)
        {
            return statusId switch
            {
                1 => OrderStatus.Pending,
                2 => OrderStatus.Processing,
                3 => OrderStatus.Shipped,
                5 => OrderStatus.Completed,
                7 => OrderStatus.Cancelled,
                8 => OrderStatus.Cancelled,
                10 => OrderStatus.Failed,
                _ => OrderStatus.Pending
            };
        }

        private int MapOrderStatusToOpenCart(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => 1,
                OrderStatus.Processing => 2,
                OrderStatus.Shipped => 3,
                OrderStatus.Completed => 5,
                OrderStatus.Cancelled => 7,
                OrderStatus.Failed => 10,
                _ => 1
            };
        }

        #endregion
    } */

    // Models moved to main OpenCart client; duplicate types removed
}
