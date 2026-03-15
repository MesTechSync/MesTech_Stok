namespace MesTech.Domain.Services;

/// <summary>
/// FEFO (First Expired First Out) siralama servisi arayuzu.
/// Depo stok cikislarinda son kullanma tarihine gore onceliklendirme yapar.
/// </summary>
public interface IFEFOSortingService
{
    /// <summary>
    /// Stok kalemlerini FEFO kuralina gore siralar.
    /// Son kullanma tarihi olmayan kalemler listenin sonuna yerlestirilir.
    /// </summary>
    IReadOnlyList<FEFOStockItem> Sort(IEnumerable<FEFOStockItem> items);

    /// <summary>
    /// Belirtilen miktar icin FEFO sirasina gore kalem secer.
    /// Secilen kalemlerin toplam miktari istenen miktari karsilar.
    /// </summary>
    IReadOnlyList<FEFOPickResult> PickForConsumption(IEnumerable<FEFOStockItem> items, decimal requiredQuantity);
}
