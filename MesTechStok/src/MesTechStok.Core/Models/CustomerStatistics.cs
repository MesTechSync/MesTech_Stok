namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Müşteri istatistiklerini tutan veri modeli
/// Dashboard ve raporlama için kullanılır
/// </summary>
public class CustomerStatistics
{
    /// <summary>
    /// Toplam müşteri sayısı
    /// </summary>
    public int TotalCustomers { get; set; }

    /// <summary>
    /// Aktif müşteri sayısı (son 6 ayda sipariş veren)
    /// </summary>
    public int ActiveCustomers { get; set; }

    /// <summary>
    /// VIP müşteri sayısı
    /// </summary>
    public int VipCustomers { get; set; }

    /// <summary>
    /// Ortalama sipariş değeri
    /// </summary>
    public decimal AverageOrderValue { get; set; }

    /// <summary>
    /// Bu ayki yeni müşteri sayısı
    /// </summary>
    public int NewCustomersThisMonth { get; set; }

    /// <summary>
    /// Bu ayki toplam sipariş değeri
    /// </summary>
    public decimal TotalOrderValueThisMonth { get; set; }
}
