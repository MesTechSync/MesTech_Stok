using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract;

/// <summary>
/// Sipariş yönetimi için servis arayüzü
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Tüm siparişleri getirir
    /// </summary>
    Task<IEnumerable<Order>> GetAllOrdersAsync();

    /// <summary>
    /// ID'ye göre sipariş getirir
    /// </summary>
    Task<Order?> GetOrderByIdAsync(int id);

    /// <summary>
    /// Sipariş numarasına göre sipariş getirir
    /// </summary>
    Task<Order?> GetOrderByNumberAsync(string orderNumber);

    /// <summary>
    /// Sipariş durumuna göre siparişleri getirir
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status);

    /// <summary>
    /// Tarih aralığına göre siparişleri getirir
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Yeni sipariş oluşturur
    /// </summary>
    Task<Order> CreateOrderAsync(Order order);

    /// <summary>
    /// Sipariş günceller
    /// </summary>
    Task<Order> UpdateOrderAsync(Order order);

    /// <summary>
    /// Sipariş durumunu günceller
    /// </summary>
    Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);

    /// <summary>
    /// Sipariş siler
    /// </summary>
    Task<bool> DeleteOrderAsync(int id);

    /// <summary>
    /// Sipariş kalemlerini getirir
    /// </summary>
    Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);

    /// <summary>
    /// Toplam sipariş sayısını getirir
    /// Dashboard istatistikleri için gerekli
    /// </summary>
    Task<int> GetTotalCountAsync();
}
