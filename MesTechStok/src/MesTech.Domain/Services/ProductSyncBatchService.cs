namespace MesTech.Domain.Services;

/// <summary>
/// 1000 ürün sync &lt; 60 saniye hedefi için batch sync domain servisi.
/// Adapter'lardan gelen ürünleri batch halinde DB'ye yazar.
/// </summary>
public sealed class ProductSyncBatchService
{
    private const int DefaultBatchSize = 100;

    /// <summary>
    /// Gelen ürün listesini batch'ler halinde işler.
    /// Her batch ayrı SaveChanges — bir batch hata alırsa diğerleri etkilenmez.
    /// </summary>
    public IReadOnlyList<(int BatchIndex, int Count)> CreateBatches<T>(
        IReadOnlyList<T> items, int batchSize = DefaultBatchSize)
    {
        var batches = new List<(int, int)>();
        for (int i = 0; i < items.Count; i += batchSize)
        {
            var count = Math.Min(batchSize, items.Count - i);
            batches.Add((i / batchSize, count));
        }
        return batches;
    }

    /// <summary>
    /// 1000 ürün → 10 batch × 100 ürün.
    /// Trendyol batch limit 100 ile uyumlu.
    /// </summary>
    public int CalculateBatchCount(int totalItems, int batchSize = DefaultBatchSize)
        => (int)Math.Ceiling((double)totalItems / batchSize);
}
