using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MesTechStok.Core.Data;
using CoreOrder = MesTechStok.Core.Data.Models.Order;
using CoreOrderItem = MesTechStok.Core.Data.Models.OrderItem;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// SQL Server destekli sipariş servisi.
    /// OrdersView tarafından kullanılan in-memory servisin yerine gerçek veritabanını kullanır.
    /// </summary>
    public class SqlBackedOrderService
    {
        private readonly AppDbContext _dbContext;

        public SqlBackedOrderService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResult<OrderItem>> GetOrdersPagedAsync(
            int page = 1,
            int pageSize = 50,
            string? searchTerm = null,
            OrderStatusFilter statusFilter = OrderStatusFilter.All,
            OrderSortOrder sortOrder = OrderSortOrder.OrderDateDesc)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;

            var query = _dbContext.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .AsQueryable();

            // Arama
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(o =>
                    o.OrderNumber.Contains(term) ||
                    (o.CustomerName != null && o.CustomerName.Contains(term)) ||
                    (o.Customer != null && o.Customer.Name.Contains(term))
                );
            }

            // Durum filtresi
            query = statusFilter switch
            {
                OrderStatusFilter.Pending => query.Where(o => o.Status == Core.Data.Models.OrderStatus.Pending),
                OrderStatusFilter.Processing => query.Where(o => o.Status == Core.Data.Models.OrderStatus.Confirmed || o.Status == Core.Data.Models.OrderStatus.Shipped),
                OrderStatusFilter.Completed => query.Where(o => o.Status == Core.Data.Models.OrderStatus.Delivered),
                OrderStatusFilter.Cancelled => query.Where(o => o.Status == Core.Data.Models.OrderStatus.Cancelled),
                _ => query
            };

            // Sıralama
            query = sortOrder switch
            {
                OrderSortOrder.OrderDate => query.OrderBy(o => o.OrderDate),
                OrderSortOrder.OrderDateDesc => query.OrderByDescending(o => o.OrderDate),
                OrderSortOrder.CustomerName => query.OrderBy(o => o.CustomerName ?? o.Customer.Name),
                OrderSortOrder.CustomerNameDesc => query.OrderByDescending(o => o.CustomerName ?? o.Customer.Name),
                OrderSortOrder.TotalAmount => query.OrderBy(o => o.TotalAmount),
                OrderSortOrder.TotalAmountDesc => query.OrderByDescending(o => o.TotalAmount),
                OrderSortOrder.Status => query.OrderBy(o => o.Status),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            var totalItems = await query.CountAsync();
            var entities = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Map to UI model (Desktop.Services.OrderItem)
            var items = entities.Select(MapToOrderItem).ToList();

            return new PagedResult<OrderItem>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };
        }

        public async Task<OrderItem?> GetOrderByIdAsync(int orderId)
        {
            var o = await _dbContext.Orders
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            return o == null ? null : MapToOrderItem(o);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var entity = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (entity == null) return false;

            entity.Status = MapToCoreStatus(newStatus);
            entity.UpdatedAt = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<OrderStatistics> GetOrderStatisticsAsync()
        {
            var totalOrders = await _dbContext.Orders.CountAsync();
            var totalValue = await _dbContext.Orders
                .Where(o => o.Status == Core.Data.Models.OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var pendingOrders = await _dbContext.Orders.CountAsync(o => o.Status == Core.Data.Models.OrderStatus.Pending);
            var completedOrders = await _dbContext.Orders.CountAsync(o => o.Status == Core.Data.Models.OrderStatus.Delivered);
            var todayOrders = await _dbContext.Orders.CountAsync(o => o.OrderDate.Date == DateTime.Today);
            var averageOrderValue = totalOrders > 0 ? (totalValue / totalOrders) : 0m;

            return new OrderStatistics
            {
                TotalOrders = totalOrders,
                TotalValue = totalValue,
                PendingOrders = pendingOrders,
                CompletedOrders = completedOrders,
                TodayOrders = todayOrders,
                AverageOrderValue = averageOrderValue
            };
        }

        private static OrderItem MapToOrderItem(CoreOrder o)
        {
            var productsList = string.Join(
                ", ",
                (o.OrderItems ?? new List<CoreOrderItem>())
                    .OrderByDescending(i => i.Quantity)
                    .Take(3)
                    .Select(i => i.ProductName)
            );

            return new OrderItem
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = !string.IsNullOrWhiteSpace(o.CustomerName) ? o.CustomerName : (o.Customer?.Name ?? ""),
                OrderDate = o.OrderDate,
                Status = MapToUiStatus(o.Status),
                TotalAmount = o.TotalAmount,
                ProductsList = productsList,
                LastUpdated = o.UpdatedAt ?? o.ModifiedDate ?? o.LastModifiedAt ?? o.CreatedAt
            };
        }

        private static OrderStatus MapToUiStatus(Core.Data.Models.OrderStatus status)
        {
            return status switch
            {
                Core.Data.Models.OrderStatus.Pending => OrderStatus.Pending,
                Core.Data.Models.OrderStatus.Confirmed => OrderStatus.Processing,
                Core.Data.Models.OrderStatus.Shipped => OrderStatus.Processing,
                Core.Data.Models.OrderStatus.Delivered => OrderStatus.Completed,
                Core.Data.Models.OrderStatus.Cancelled => OrderStatus.Cancelled,
                _ => OrderStatus.Pending
            };
        }

        private static Core.Data.Models.OrderStatus MapToCoreStatus(OrderStatus uiStatus)
        {
            return uiStatus switch
            {
                OrderStatus.Pending => Core.Data.Models.OrderStatus.Pending,
                OrderStatus.Processing => Core.Data.Models.OrderStatus.Confirmed,
                OrderStatus.Completed => Core.Data.Models.OrderStatus.Delivered,
                OrderStatus.Cancelled => Core.Data.Models.OrderStatus.Cancelled,
                _ => Core.Data.Models.OrderStatus.Pending
            };
        }
    }
}


