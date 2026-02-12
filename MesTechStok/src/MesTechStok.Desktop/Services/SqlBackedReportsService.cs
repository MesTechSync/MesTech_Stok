using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MesTechStok.Core.Data;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// SQL destekli raporlama servisi: kritik stok ve en çok satanlar vb.
    /// </summary>
    public class SqlBackedReportsService
    {
        private readonly AppDbContext _dbContext;

        public SqlBackedReportsService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<(DateTime Date, decimal Revenue)>> GetDailyRevenueAsync(int days = 15)
        {
            if (days <= 0) days = 15;
            var fromDate = DateTime.Today.AddDays(-(days - 1));

            var raw = await _dbContext.Orders
                .AsNoTracking()
                .Where(o => o.Status == Core.Data.Models.OrderStatus.Delivered && o.OrderDate.Date >= fromDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var map = raw.ToDictionary(x => x.Date, x => x.Revenue);
            var result = new List<(DateTime, decimal)>();
            for (var day = 0; day < days; day++)
            {
                var d = fromDate.AddDays(day);
                map.TryGetValue(d, out var rev);
                result.Add((d, rev));
            }
            return result;
        }

        public async Task<(List<(string Name, int Sales)> TopProducts, List<(string Name, int Stock)> LowStockItems, decimal TotalRevenue, int TotalSales, decimal StockValue)>
            GetDashboardSummariesAsync()
        {
            // Basit SQL tabanlı özetler (örnek)
            var topProducts = await _dbContext.OrderItems
                .AsNoTracking()
                .GroupBy(oi => oi.ProductName)
                .Select(g => new { Name = g.Key, Sales = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Sales)
                .Take(10)
                .ToListAsync();

            var lowStock = await _dbContext.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
                .OrderBy(p => p.Stock)
                .Select(p => new { p.Name, p.Stock })
                .Take(20)
                .ToListAsync();

            var totalRevenue = await _dbContext.Orders
                .AsNoTracking()
                .Where(o => o.Status == Core.Data.Models.OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var totalSales = await _dbContext.Orders.AsNoTracking().CountAsync();
            var stockValue = await _dbContext.Products.AsNoTracking().SumAsync(p => (decimal?)p.Stock * p.SalePrice) ?? 0m;

            return (
                TopProducts: topProducts.Select(x => (x.Name, x.Sales)).ToList(),
                LowStockItems: lowStock.Select(x => (x.Name, x.Stock)).ToList(),
                TotalRevenue: totalRevenue,
                TotalSales: totalSales,
                StockValue: stockValue
            );
        }
    }
}


