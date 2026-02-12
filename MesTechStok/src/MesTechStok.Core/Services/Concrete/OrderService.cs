using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Core.Services.Concrete;

/// <summary>
/// Sipariş yönetimi servisi implementasyonu
/// </summary>
public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        // Sipariş numarası benzersizlik kontrolü
        if (!string.IsNullOrEmpty(order.OrderNumber))
        {
            var existingOrder = await GetOrderByNumberAsync(order.OrderNumber);
            if (existingOrder != null)
                throw new InvalidOperationException($"Sipariş numarası '{order.OrderNumber}' zaten kullanılıyor.");
        }
        else
        {
            // Otomatik sipariş numarası oluştur
            order.OrderNumber = await GenerateOrderNumberAsync();
        }

        order.OrderDate = DateTime.UtcNow;
        order.OrderDate = order.OrderDate == default ? DateTime.UtcNow : order.OrderDate;

        // Sipariş kalemlerini kontrol et
        foreach (var item in order.OrderItems)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Ürün ID '{item.ProductId}' bulunamadı.");

            // Fiyat hesaplama
            item.UnitPrice = item.UnitPrice > 0 ? item.UnitPrice : product.SalePrice;
            item.TotalPrice = item.Quantity * item.UnitPrice;
        }

        // Toplam tutar hesaplama
        order.TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<Order> UpdateOrderAsync(Order order)
    {
        var existingOrder = await GetOrderByIdAsync(order.Id);
        if (existingOrder == null)
            throw new InvalidOperationException($"ID '{order.Id}' ile sipariş bulunamadı.");

        order.UpdatedAt = DateTime.UtcNow;

        // Sipariş kalemleri güncelleme
        foreach (var item in order.OrderItems)
        {
            item.TotalPrice = item.Quantity * item.UnitPrice;
        }

        // Toplam tutar yeniden hesaplama
        order.TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice);

        _context.Orders.Update(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
            return false;

        var previousStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        // Durum değişikliği özel işlemleri
        switch (newStatus)
        {
            case OrderStatus.Confirmed:
                await ProcessOrderConfirmationAsync(order);
                await ProcessOrderProcessingAsync(order);
                break;
            case OrderStatus.Shipped:
                await ProcessOrderShippingAsync(order);
                break;
            case OrderStatus.Delivered:
                await ProcessOrderDeliveryAsync(order);
                break;
            case OrderStatus.Cancelled:
                await ProcessOrderCancellationAsync(order, previousStatus);
                break;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await GetOrderByIdAsync(id);
        if (order == null)
            return false;

        // Sadece bekleyen veya iptal edilen siparişler silinebilir
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Cancelled)
            throw new InvalidOperationException("Sadece bekleyen veya iptal edilen siparişler silinebilir.");

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync();
    }

    /// <summary>
    /// Toplam sipariş sayısını getirir
    /// Dashboard istatistikleri için gerekli
    /// </summary>
    public async Task<int> GetTotalCountAsync()
    {
        try
        {
            return await _context.Orders.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total order count");
            throw;
        }
    }

    #region Private Helper Methods

    private async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"ORD{today:yyyyMMdd}";

        var lastOrder = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        if (lastOrder == null)
        {
            return $"{prefix}001";
        }

        var lastNumber = lastOrder.OrderNumber.Substring(prefix.Length);
        if (int.TryParse(lastNumber, out int number))
        {
            return $"{prefix}{(number + 1):D3}";
        }

        return $"{prefix}001";
    }

    private async Task ProcessOrderConfirmationAsync(Order order)
    {
        // Stok rezervasyonu yapılabilir
        foreach (var item in order.OrderItems)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null && product.Stock < item.Quantity)
            {
                throw new InvalidOperationException($"'{product.Name}' ürünü için yetersiz stok. Mevcut: {product.Stock}, İstenen: {item.Quantity}");
            }
        }
    }

    private async Task ProcessOrderProcessingAsync(Order order)
    {
        // Stok düşümü yapılabilir
        foreach (var item in order.OrderItems)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                if (product.Stock < item.Quantity)
                {
                    throw new InvalidOperationException($"'{product.Name}' ürünü için yetersiz stok.");
                }

                product.Stock -= item.Quantity;
                product.ModifiedDate = DateTime.UtcNow;

                // Stok hareket kaydı
                var stockMovement = new StockMovement
                {
                    ProductId = product.Id,
                    MovementType = "OUT",
                    Quantity = item.Quantity,
                    PreviousStock = product.Stock + item.Quantity,
                    NewStock = product.Stock,
                    DocumentNumber = order.OrderNumber,
                    Notes = $"Sipariş işleme - {order.OrderNumber}",
                    Date = DateTime.UtcNow
                };

                _context.StockMovements.Add(stockMovement);
            }
        }
    }

    private async Task ProcessOrderShippingAsync(Order order)
    {
        // Kargo işlemleri burada yapılabilir
        await Task.CompletedTask;
    }

    private async Task ProcessOrderDeliveryAsync(Order order)
    {
        // Teslimat işlemleri burada yapılabilir
        await Task.CompletedTask;
    }

    private async Task ProcessOrderCancellationAsync(Order order, OrderStatus previousStatus)
    {
        // Eğer sipariş işleme alınmışsa stok iadesi yap
        if (previousStatus == OrderStatus.Confirmed || previousStatus == OrderStatus.Shipped)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                    product.ModifiedDate = DateTime.UtcNow;

                    // Stok hareket kaydı
                    var stockMovement = new StockMovement
                    {
                        ProductId = product.Id,
                        MovementType = "IN",
                        Quantity = item.Quantity,
                        PreviousStock = product.Stock - item.Quantity,
                        NewStock = product.Stock,
                        DocumentNumber = order.OrderNumber,
                        Notes = $"Sipariş iptali - {order.OrderNumber}",
                        Date = DateTime.UtcNow
                    };

                    _context.StockMovements.Add(stockMovement);
                }
            }
        }
    }

    #endregion
}
