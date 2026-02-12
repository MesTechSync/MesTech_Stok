using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Abstract;
using MesTechStok.Core.Integrations.OpenCart.Dtos;
using MesTechStok.Core.Integrations.OpenCart;

namespace MesTechStok.Core.Integrations.OpenCart
{
    public class OpenCartSyncService : IOpenCartSyncService
    {
        private readonly IOpenCartClient _openCartClient;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<OpenCartSyncService> _logger;
        private readonly ISyncHealthProvider? _health;

        private bool _isSyncRunning = false;
        private DateTime? _lastSyncDate = null;
        private CancellationTokenSource? _autoSyncCts;
        private Task? _autoSyncTask;

        // Interface Events
        public event EventHandler<SyncStartedEventArgs>? SyncStarted;
        public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
        public event EventHandler<SyncErrorEventArgs>? SyncError;

        // Interface Properties
        public bool IsSyncRunning => _isSyncRunning;
        public DateTime? LastSyncDate => _lastSyncDate;

        public OpenCartSyncService(
            IOpenCartClient openCartClient,
            IProductService productService,
            IOrderService orderService,
            IInventoryService inventoryService,
            ILogger<OpenCartSyncService> logger,
            ISyncHealthProvider? health = null)
        {
            _openCartClient = openCartClient ?? throw new ArgumentNullException(nameof(openCartClient));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _health = health;
        }

        // Interface Methods
        public async Task<OpenCartSyncResult> SyncProductsFromOpenCartAsync()
        {
            var syncType = "ProductsFromOpenCart";
            var startTime = DateTime.UtcNow;

            OnSyncStarted(new SyncStartedEventArgs { SyncType = syncType, StartTime = startTime });
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();
            _logger.LogInformation("[Sync:{Sync}] CorrelationId={CorrelationId} started", syncType, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            _isSyncRunning = true;

            var result = new OpenCartSyncResult
            {
                SyncDate = startTime
            };

            try
            {
                _logger.LogInformation("Starting products sync from OpenCart...");

                var OpenCartProducts = await _openCartClient.GetAllProductsAsync() ?? Enumerable.Empty<OpenCartProduct>();
                var localProducts = await _productService.GetAllProductsAsync();

                foreach (var OpenCartProduct in OpenCartProducts)
                {
                    try
                    {
                        var existingProduct = localProducts.FirstOrDefault(p =>
                            (!string.IsNullOrEmpty(p.SKU) && p.SKU == (OpenCartProduct.SKU() ?? string.Empty)) ||
                            (p.OpenCartProductId.HasValue && p.OpenCartProductId.Value == OpenCartProduct.ProductId));

                        if (existingProduct != null)
                        {
                            // Update existing product
                            await UpdatelocalProductFromOpenCart(existingProduct, OpenCartProduct);
                            result.SuccessCount++;
                            _logger.LogDebug($"Updated product: {OpenCartProduct.Name}");
                        }
                        else
                        {
                            // Create new product
                            await CreatelocalProductFromOpenCart(OpenCartProduct);
                            result.SuccessCount++;
                            _logger.LogDebug($"Created product: {OpenCartProduct.Name}");
                        }

                        result.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error processing product {OpenCartProduct.Name}: {ex.Message}");
                        _logger.LogError(ex, $"Error processing product {OpenCartProduct.Name}");
                    }
                }

                result.IsSuccess = result.ErrorCount == 0;
                _lastSyncDate = DateTime.UtcNow;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation($"Products sync completed. Processed: {result.TotalProcessed}, Success: {result.SuccessCount}, Errors: {result.ErrorCount}");

                OnSyncCompleted(new SyncCompletedEventArgs
                {
                    SyncType = syncType,
                    Result = result,
                    Duration = result.Duration
                });
                if (result.IsSuccess) _health?.MarkSuccess(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId); else _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add(ex.Message);
                result.EndTime = DateTime.UtcNow;

                OnSyncError(new SyncErrorEventArgs
                {
                    SyncType = syncType,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                _logger.LogError(ex, "Failed to sync products from OpenCart");
            }
            finally
            {
                _isSyncRunning = false;
            }

            return result;
        }

        public async Task<OpenCartSyncResult> SyncProductsToOpenCartAsync()
        {
            var syncType = "ProductsToOpenCart";
            var startTime = DateTime.UtcNow;

            OnSyncStarted(new SyncStartedEventArgs { SyncType = syncType, StartTime = startTime });
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();
            _logger.LogInformation("[Sync:{Sync}] CorrelationId={CorrelationId} started", syncType, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            _isSyncRunning = true;

            var result = new OpenCartSyncResult
            {
                SyncDate = startTime
            };

            try
            {
                _logger.LogInformation("Starting products sync to OpenCart...");

                var localProducts = await _productService.GetAllProductsAsync();
                var OpenCartProducts = await _openCartClient.GetAllProductsAsync() ?? Enumerable.Empty<OpenCartProduct>();

                foreach (var localProduct in localProducts)
                {
                    try
                    {
                        var existingOpenCartProduct = OpenCartProducts.FirstOrDefault(p =>
                            (p.SKU() ?? string.Empty) == (localProduct.SKU ?? string.Empty) ||
                            (localProduct.OpenCartProductId.HasValue && p.ProductId == localProduct.OpenCartProductId.Value));

                        if (existingOpenCartProduct != null && localProduct.OpenCartProductId.HasValue)
                        {
                            // Update existing OpenCart product
                            var updatedProduct = MaplocalProductToOpenCart(localProduct, existingOpenCartProduct);
                            await _openCartClient.UpdateProductAsync(localProduct.OpenCartProductId.Value, updatedProduct);
                            result.SuccessCount++;
                            _logger.LogDebug($"Updated OpenCart product: {localProduct.Name}");
                        }
                        else if (!localProduct.OpenCartProductId.HasValue)
                        {
                            // Create new OpenCart product
                            var newProduct = MaplocalProductToOpenCart(localProduct);
                            var createdProductId = await _openCartClient.CreateProductAsync(newProduct);

                            if (createdProductId.HasValue)
                            {
                                // Update local product with OpenCart ID
                                localProduct.OpenCartProductId = createdProductId.Value;
                                await _productService.UpdateProductAsync(localProduct);

                                result.SuccessCount++;
                                _logger.LogDebug($"Created OpenCart product: {localProduct.Name}");
                            }
                        }

                        result.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error processing product {localProduct.Name}: {ex.Message}");
                        _logger.LogError(ex, $"Error processing product {localProduct.Name}");
                    }
                }

                result.IsSuccess = result.ErrorCount == 0;
                _lastSyncDate = DateTime.UtcNow;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation($"Products sync to OpenCart completed. Processed: {result.TotalProcessed}, Success: {result.SuccessCount}, Errors: {result.ErrorCount}");

                OnSyncCompleted(new SyncCompletedEventArgs
                {
                    SyncType = syncType,
                    Result = result,
                    Duration = result.Duration
                });
                if (result.IsSuccess) _health?.MarkSuccess(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId); else _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add(ex.Message);
                result.EndTime = DateTime.UtcNow;

                OnSyncError(new SyncErrorEventArgs
                {
                    SyncType = syncType,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                _logger.LogError(ex, "Failed to sync products to OpenCart");
            }
            finally
            {
                _isSyncRunning = false;
            }

            return result;
        }

        public async Task<OpenCartSyncResult> SyncStockLevelsAsync()
        {
            var syncType = "StockLevels";
            var startTime = DateTime.UtcNow;

            OnSyncStarted(new SyncStartedEventArgs { SyncType = syncType, StartTime = startTime });
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();
            _logger.LogInformation("[Sync:{Sync}] CorrelationId={CorrelationId} started", syncType, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            _isSyncRunning = true;

            var result = new OpenCartSyncResult
            {
                SyncDate = startTime
            };

            try
            {
                _logger.LogInformation("Starting stock levels sync...");

                var localProducts = await _productService.GetAllProductsAsync();
                var stockUpdates = new List<OpenCartStockUpdate>();

                foreach (var product in localProducts.Where(p => p.OpenCartProductId.HasValue))
                {
                    try
                    {
                        stockUpdates.Add(new OpenCartStockUpdate
                        {
                            ProductId = product.OpenCartProductId.Value,
                            Quantity = product.Stock
                        });
                        result.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error preparing stock update for product {product.Name}: {ex.Message}");
                        _logger.LogError(ex, $"Error preparing stock update for product {product.Name}");
                    }
                }

                if (stockUpdates.Any())
                {
                    var syncResult = await _openCartClient.BulkUpdateStockAsync(stockUpdates);
                    if (syncResult.IsSuccess)
                    {
                        result.SuccessCount = stockUpdates.Count;
                        result.IsSuccess = true;
                        _logger.LogInformation($"Stock levels sync completed. Updated {stockUpdates.Count} products.");
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.Errors.AddRange(syncResult.Errors);
                        _logger.LogError("Bulk stock update failed");
                    }
                }
                else
                {
                    result.IsSuccess = true;
                    _logger.LogInformation("No stock updates required.");
                }

                _lastSyncDate = DateTime.UtcNow;
                result.EndTime = DateTime.UtcNow;

                OnSyncCompleted(new SyncCompletedEventArgs
                {
                    SyncType = syncType,
                    Result = result,
                    Duration = result.Duration
                });
                if (result.IsSuccess) _health?.MarkSuccess(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId); else _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add(ex.Message);
                result.EndTime = DateTime.UtcNow;

                OnSyncError(new SyncErrorEventArgs
                {
                    SyncType = syncType,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                _logger.LogError(ex, "Failed to sync stock levels");
            }
            finally
            {
                _isSyncRunning = false;
            }

            return result;
        }

        public async Task<OpenCartSyncResult> SyncOrdersFromOpenCartAsync()
        {
            var syncType = "OrdersFromOpenCart";
            var startTime = DateTime.UtcNow;

            OnSyncStarted(new SyncStartedEventArgs { SyncType = syncType, StartTime = startTime });
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();
            _logger.LogInformation("[Sync:{Sync}] CorrelationId={CorrelationId} started", syncType, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            _isSyncRunning = true;

            var result = new OpenCartSyncResult
            {
                SyncDate = startTime
            };

            try
            {
                _logger.LogInformation("Starting orders sync from OpenCart...");

                var openCartOrders = await _openCartClient.GetAllOrdersAsync() ?? Enumerable.Empty<OpenCartOrder>();
                var localOrders = await _orderService.GetAllOrdersAsync();

                foreach (var openCartOrder in openCartOrders)
                {
                    try
                    {
                        var existingOrder = localOrders.FirstOrDefault(o => o.OpenCartOrderId == openCartOrder.OrderId);

                        if (existingOrder != null)
                        {
                            // Update existing order
                            await UpdateLocalOrderFromOpenCart(existingOrder, openCartOrder);
                            result.SuccessCount++;
                            _logger.LogDebug($"Updated order: {openCartOrder.OrderId}");
                        }
                        else
                        {
                            // Create new order
                            await CreateLocalOrderFromOpenCart(openCartOrder);
                            result.SuccessCount++;
                            _logger.LogDebug($"Created order: {openCartOrder.OrderId}");
                        }

                        result.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error processing order {openCartOrder.OrderId}: {ex.Message}");
                        _logger.LogError(ex, $"Error processing order {openCartOrder.OrderId}");
                    }
                }

                result.IsSuccess = result.ErrorCount == 0;
                _lastSyncDate = DateTime.UtcNow;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation($"Orders sync completed. Processed: {result.TotalProcessed}, Success: {result.SuccessCount}, Errors: {result.ErrorCount}");

                OnSyncCompleted(new SyncCompletedEventArgs
                {
                    SyncType = syncType,
                    Result = result,
                    Duration = result.Duration
                });
                if (result.IsSuccess) _health?.MarkSuccess(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId); else _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add(ex.Message);
                result.EndTime = DateTime.UtcNow;

                OnSyncError(new SyncErrorEventArgs
                {
                    SyncType = syncType,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                _logger.LogError(ex, "Failed to sync orders from OpenCart");
            }
            finally
            {
                _isSyncRunning = false;
            }

            return result;
        }

        public async Task<OpenCartSyncResult> SyncOrderStatusToOpenCartAsync()
        {
            var syncType = "OrderStatusToOpenCart";
            var startTime = DateTime.UtcNow;

            OnSyncStarted(new SyncStartedEventArgs { SyncType = syncType, StartTime = startTime });
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();
            _logger.LogInformation("[Sync:{Sync}] CorrelationId={CorrelationId} started", syncType, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            _isSyncRunning = true;

            var result = new OpenCartSyncResult
            {
                SyncDate = startTime
            };

            try
            {
                _logger.LogInformation("Starting order status sync to OpenCart...");

                var localOrders = await _orderService.GetAllOrdersAsync();
                var since = _lastSyncDate ?? DateTime.MinValue;
                var ordersToUpdate = localOrders.Where(o => o.OpenCartOrderId.HasValue && o.ModifiedDate > since);

                foreach (var order in ordersToUpdate)
                {
                    try
                    {
                        var openCartStatus = OpenCartExtensions.MapOrderStatusToOpenCart(order.Status);
                        var success = await _openCartClient.UpdateOrderStatusAsync(order.OpenCartOrderId.Value, openCartStatus.ToString());

                        if (success)
                        {
                            result.SuccessCount++;
                            _logger.LogDebug($"Updated order status: {order.OrderNumber}");
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"Failed to update order status: {order.OrderNumber}");
                        }

                        result.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error updating order status {order.OrderNumber}: {ex.Message}");
                        _logger.LogError(ex, $"Error updating order status {order.OrderNumber}");
                    }
                }

                result.IsSuccess = result.ErrorCount == 0;
                _lastSyncDate = DateTime.UtcNow;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation($"Order status sync completed. Processed: {result.TotalProcessed}, Success: {result.SuccessCount}, Errors: {result.ErrorCount}");

                OnSyncCompleted(new SyncCompletedEventArgs
                {
                    SyncType = syncType,
                    Result = result,
                    Duration = result.Duration
                });
                if (result.IsSuccess) _health?.MarkSuccess(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId); else _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add(ex.Message);
                result.EndTime = DateTime.UtcNow;

                OnSyncError(new SyncErrorEventArgs
                {
                    SyncType = syncType,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                _health?.MarkFailure(MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                _logger.LogError(ex, "Failed to sync order status to OpenCart");
            }
            finally
            {
                _isSyncRunning = false;
            }

            return result;
        }

        public async Task<OpenCartSyncResult> FullSyncAsync()
        {
            var syncType = "FullSync";
            var startTime = DateTime.UtcNow;

            OnSyncStarted(new SyncStartedEventArgs { SyncType = syncType, StartTime = startTime });
            _isSyncRunning = true;

            var result = new OpenCartSyncResult
            {
                SyncDate = startTime
            };

            try
            {
                _logger.LogInformation("Starting full synchronization...");

                // 1. Sync products from OpenCart
                var productsFromResult = await SyncProductsFromOpenCartAsync();
                result.TotalProcessed += productsFromResult.TotalProcessed;
                result.SuccessCount += productsFromResult.SuccessCount;
                result.ErrorCount += productsFromResult.ErrorCount;
                result.Errors.AddRange(productsFromResult.Errors);

                // 2. Sync products to OpenCart
                var productsToResult = await SyncProductsToOpenCartAsync();
                result.TotalProcessed += productsToResult.TotalProcessed;
                result.SuccessCount += productsToResult.SuccessCount;
                result.ErrorCount += productsToResult.ErrorCount;
                result.Errors.AddRange(productsToResult.Errors);

                // 3. Sync orders
                var ordersResult = await SyncOrdersFromOpenCartAsync();
                result.TotalProcessed += ordersResult.TotalProcessed;
                result.SuccessCount += ordersResult.SuccessCount;
                result.ErrorCount += ordersResult.ErrorCount;
                result.Errors.AddRange(ordersResult.Errors);

                // 4. Sync stock levels
                var stockResult = await SyncStockLevelsAsync();
                result.TotalProcessed += stockResult.TotalProcessed;
                result.SuccessCount += stockResult.SuccessCount;
                result.ErrorCount += stockResult.ErrorCount;
                result.Errors.AddRange(stockResult.Errors);

                result.IsSuccess = result.ErrorCount == 0;
                _lastSyncDate = DateTime.UtcNow;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation($"Full sync completed. Processed: {result.TotalProcessed}, Success: {result.SuccessCount}, Errors: {result.ErrorCount}");

                OnSyncCompleted(new SyncCompletedEventArgs
                {
                    SyncType = syncType,
                    Result = result,
                    Duration = result.Duration
                });
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add(ex.Message);
                result.EndTime = DateTime.UtcNow;

                OnSyncError(new SyncErrorEventArgs
                {
                    SyncType = syncType,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });

                _logger.LogError(ex, "Failed to complete full sync");
            }
            finally
            {
                _isSyncRunning = false;
            }

            return result;
        }

        public async Task<bool> StartAutoSyncAsync(TimeSpan interval)
        {
            if (interval < TimeSpan.FromSeconds(10)) interval = TimeSpan.FromSeconds(10);
            if (_autoSyncTask != null && !_autoSyncTask.IsCompleted)
            {
                _logger.LogWarning("Auto sync already running");
                return false;
            }
            _autoSyncCts = new CancellationTokenSource();
            var token = _autoSyncCts.Token;
            _autoSyncTask = Task.Run(async () =>
            {
                _logger.LogInformation($"Auto sync loop started (interval={interval})");
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (!_isSyncRunning)
                        {
                            await SyncStockLevelsAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Auto sync iteration failed");
                    }
                    await Task.Delay(interval, token).ContinueWith(_ => { });
                }
                _logger.LogInformation("Auto sync loop terminated");
            }, token);
            return true;
        }

        public async Task<bool> StopAutoSyncAsync()
        {
            if (_autoSyncCts == null)
            {
                _logger.LogInformation("Auto sync not active");
                return false;
            }
            _autoSyncCts.Cancel();
            try { if (_autoSyncTask != null) await _autoSyncTask; } catch { }
            _autoSyncCts.Dispose();
            _autoSyncCts = null; _autoSyncTask = null;
            _logger.LogInformation("Auto sync stopped");
            return true;
        }

        // Event handlers
        protected virtual void OnSyncStarted(SyncStartedEventArgs e)
        {
            SyncStarted?.Invoke(this, e);
        }

        protected virtual void OnSyncCompleted(SyncCompletedEventArgs e)
        {
            SyncCompleted?.Invoke(this, e);
        }

        protected virtual void OnSyncError(SyncErrorEventArgs e)
        {
            SyncError?.Invoke(this, e);
        }

        private async Task UpdatelocalProductFromOpenCart(Product localProduct, OpenCartProduct OpenCartProduct)
        {
            localProduct.Name = OpenCartProduct.Name();
            localProduct.Description = OpenCartProduct.Description();
            localProduct.SalePrice = OpenCartProduct.SalePrice();
            localProduct.Stock = OpenCartProduct.Quantity;
            localProduct.OpenCartProductId = OpenCartProduct.ProductId;
            localProduct.LastModifiedAt = DateTime.Now;

            await _productService.UpdateProductAsync(localProduct);
        }

        private async Task CreatelocalProductFromOpenCart(OpenCartProduct OpenCartProduct)
        {
            var newProduct = new Product
            {
                Name = OpenCartProduct.Name(),
                Description = OpenCartProduct.Description(),
                SKU = OpenCartProduct.SKU(),
                Barcode = OpenCartProduct.Ean ?? OpenCartProduct.Upc,
                SalePrice = OpenCartProduct.SalePrice(),
                Stock = OpenCartProduct.Quantity,
                OpenCartProductId = OpenCartProduct.ProductId,
                ModifiedDate = DateTime.Now
            };

            await _productService.CreateProductAsync(newProduct);
        }

        private OpenCartProduct MaplocalProductToOpenCart(Product localProduct, OpenCartProduct existingProduct = null)
        {
            return new OpenCartProduct
            {
                ProductId = existingProduct?.ProductId ?? 0,
                // Name and Description will be set through ProductDescriptions
                Model = localProduct.SKU,
                Price = localProduct.SalePrice,
                Quantity = localProduct.Stock,
                Ean = localProduct.Barcode,
                Status = existingProduct?.Status ?? true,
                DateAdded = existingProduct?.DateAdded ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DateModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        private async Task UpdateLocalOrderFromOpenCart(Order localOrder, OpenCartOrder openCartOrder)
        {
            localOrder.Status = MapOpenCartOrderStatus(openCartOrder.OrderStatus);
            localOrder.LastModifiedAt = DateTime.Now;

            await _orderService.UpdateOrderAsync(localOrder);
        }

        private async Task CreateLocalOrderFromOpenCart(OpenCartOrder openCartOrder)
        {
            var newOrder = new Order
            {
                OrderNumber = openCartOrder.OrderId.ToString(),
                OpenCartOrderId = openCartOrder.OrderId,
                CustomerName = openCartOrder.CustomerName(),
                CustomerEmail = openCartOrder.CustomerEmail(),
                Status = MapOpenCartOrderStatus(openCartOrder.OrderStatus),
                TotalAmount = openCartOrder.Total,
                OrderDate = DateTime.TryParse(openCartOrder.DateAdded, out var parsedDate) ? parsedDate : DateTime.Now,
                LastModifiedAt = DateTime.Now,
                OrderItems = openCartOrder.Products.Select(p => new OrderItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Quantity = p.Quantity,
                    UnitPrice = p.SalePrice(),
                    TotalPrice = p.Total
                }).ToList()
            };

            await _orderService.CreateOrderAsync(newOrder);
        }

        private OrderStatus MapOpenCartOrderStatus(string openCartStatus)
        {
            return openCartStatus?.ToLower() switch
            {
                "pending" => OrderStatus.Pending,
                "processing" => OrderStatus.Confirmed,
                "shipped" => OrderStatus.Shipped,
                "delivered" => OrderStatus.Delivered,
                "cancelled" => OrderStatus.Cancelled,
                "complete" => OrderStatus.Delivered,
                _ => OrderStatus.Pending
            };
        }
    }
}
