using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services
{
    public class EnhancedOrderService
    {
        private readonly List<OrderItem> _allOrders;
        private readonly Random _random = new();

        public EnhancedOrderService()
        {
            _allOrders = GenerateOrderData();
        }

        #region Public Methods

        public async Task<PagedResult<OrderItem>> GetOrdersPagedAsync(
            int page = 1,
            int pageSize = 50,
            string? searchTerm = null,
            OrderStatusFilter statusFilter = OrderStatusFilter.All,
            OrderSortOrder sortOrder = OrderSortOrder.OrderDateDesc)
        {
            await Task.Delay(40); // Simulate network delay

            var filteredOrders = FilterOrders(searchTerm, statusFilter);
            var sortedOrders = SortOrders(filteredOrders, sortOrder);

            var totalItems = sortedOrders.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = sortedOrders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<OrderItem>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<OrderItem?> GetOrderByIdAsync(int orderId)
        {
            await Task.Delay(25);
            return _allOrders.FirstOrDefault(o => o.Id == orderId);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            await Task.Delay(50);

            var order = _allOrders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return false;

            order.Status = newStatus;
            order.LastUpdated = DateTime.Now;

            return true;
        }

        public async Task<OrderStatistics> GetOrderStatisticsAsync()
        {
            await Task.Delay(75);

            var totalOrders = _allOrders.Count;
            var totalValue = _allOrders.Sum(o => o.TotalAmount);
            var pendingOrders = _allOrders.Count(o => o.Status == OrderStatus.Pending);
            var completedOrders = _allOrders.Count(o => o.Status == OrderStatus.Completed);
            var todayOrders = _allOrders.Count(o => o.OrderDate.Date == DateTime.Today);
            var averageOrderValue = _allOrders.Any() ? _allOrders.Average(o => o.TotalAmount) : 0;

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

        #endregion

        #region Private Methods

        private IEnumerable<OrderItem> FilterOrders(string? searchTerm, OrderStatusFilter statusFilter)
        {
            var orders = _allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                orders = orders.Where(o =>
                    o.CustomerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    o.Id.ToString().Contains(searchTerm) ||
                    o.ProductsList.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            orders = statusFilter switch
            {
                OrderStatusFilter.Pending => orders.Where(o => o.Status == OrderStatus.Pending),
                OrderStatusFilter.Processing => orders.Where(o => o.Status == OrderStatus.Processing),
                OrderStatusFilter.Completed => orders.Where(o => o.Status == OrderStatus.Completed),
                OrderStatusFilter.Cancelled => orders.Where(o => o.Status == OrderStatus.Cancelled),
                _ => orders
            };

            return orders;
        }

        private IEnumerable<OrderItem> SortOrders(IEnumerable<OrderItem> orders, OrderSortOrder sortOrder)
        {
            return sortOrder switch
            {
                OrderSortOrder.OrderDate => orders.OrderBy(o => o.OrderDate),
                OrderSortOrder.OrderDateDesc => orders.OrderByDescending(o => o.OrderDate),
                OrderSortOrder.CustomerName => orders.OrderBy(o => o.CustomerName),
                OrderSortOrder.CustomerNameDesc => orders.OrderByDescending(o => o.CustomerName),
                OrderSortOrder.TotalAmount => orders.OrderBy(o => o.TotalAmount),
                OrderSortOrder.TotalAmountDesc => orders.OrderByDescending(o => o.TotalAmount),
                OrderSortOrder.Status => orders.OrderBy(o => o.Status),
                _ => orders.OrderByDescending(o => o.OrderDate)
            };
        }

        private List<OrderItem> GenerateOrderData()
        {
            var orders = new List<OrderItem>();
            var customerNames = new[] { "Ahmet Yılmaz", "Fatma Kaya", "Mehmet Demir", "Ayşe Şahin", "Mustafa Çelik", "Zeynep Arslan", "Ali Koç", "Elif Doğan", "Osman Türk", "Hatice Avcı", "İbrahim Güler", "Sevgi Polat", "Hasan Kara", "Emine Yıldız", "Hüseyin Özkan", "Nermin Özdemir", "Kadir Aksoy", "Seda Yalçın", "Recep Kılıç", "Gül Bayram" };
            var productLists = new[] { "Samsung S24, Kılıf", "iPhone 15, Kulaklık", "Coca Cola x5, Doritos x3", "Nivea Krem, L'Oreal Şampuan", "Nike Ayakkabı, Spor Çantası", "MacBook Pro, Mouse", "Nutella, Çikolata x3", "Protein Tozu, Dumbbell", "Çay 100'lü, Kahve", "iPad Air, Apple Pencil" };

            for (int i = 1; i <= 250; i++)
            {
                var orderDate = DateTime.Now.AddDays(-_random.Next(0, 90));
                var status = GenerateRandomOrderStatus();

                var order = new OrderItem
                {
                    Id = i,
                    CustomerName = customerNames[_random.Next(customerNames.Length)],
                    OrderDate = orderDate,
                    Status = status,
                    TotalAmount = GenerateRandomOrderAmount(),
                    ProductsList = productLists[_random.Next(productLists.Length)],
                    LastUpdated = orderDate.AddHours(_random.Next(1, 24))
                };

                orders.Add(order);
            }

            return orders;
        }

        private OrderStatus GenerateRandomOrderStatus()
        {
            var statuses = Enum.GetValues<OrderStatus>();
            return statuses[_random.Next(statuses.Length)];
        }

        private decimal GenerateRandomOrderAmount()
        {
            return (decimal)(_random.Next(50, 5000) + _random.NextDouble());
        }

        #endregion
    }

    #region Supporting Classes

    public class OrderItem
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string ProductsList { get; set; } = "";
        public DateTime LastUpdated { get; set; }

        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    OrderStatus.Pending => "⏳",
                    OrderStatus.Processing => "🔄",
                    OrderStatus.Completed => "✅",
                    OrderStatus.Cancelled => "❌",
                    _ => "📦"
                };
            }
        }

        public string FormattedAmount => $"₺{TotalAmount:N2}";
        public string FormattedDate => OrderDate.ToString("dd.MM.yyyy");
        public string FormattedLastUpdate => LastUpdated.ToString("dd.MM.yyyy HH:mm");
    }

    public class OrderStatistics
    {
        public int TotalOrders { get; set; }
        public decimal TotalValue { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int TodayOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Cancelled
    }

    public enum OrderStatusFilter
    {
        All,
        Pending,
        Processing,
        Completed,
        Cancelled
    }

    public enum OrderSortOrder
    {
        OrderDate,
        OrderDateDesc,
        CustomerName,
        CustomerNameDesc,
        TotalAmount,
        TotalAmountDesc,
        Status
    }

    #endregion
}