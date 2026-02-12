using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Core.Services.Concrete;

/// <summary>
/// Envanter yönetimi servisi implementasyonu
/// Stok hareketleri, barkod tarama ve gerçek zamanlı güncellemeler
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    private readonly IProductService _productService;
    private readonly ILogger<InventoryService>? _logger;

    /// <summary>
    /// Complete constructor with all dependencies
    /// </summary>
    public InventoryService(AppDbContext context, IProductService productService, ILogger<InventoryService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StockMovement?> ProcessBarcodeSaleAsync(string barcode, int quantity, string? DocumentNumber = null, string? notes = null)
    {
        var product = await _productService.GetProductByBarcodeAsync(barcode);
        if (product == null)
            throw new InvalidOperationException($"Barkod '{barcode}' ile ürün bulunamadı.");

        if (product.Stock < quantity)
            throw new InvalidOperationException($"Yetersiz stok. Mevcut: {product.Stock}, İstenen: {quantity}");

        return await RemoveStockAsync(product.Id, quantity, DocumentNumber, notes, "Barkod Satış");
    }

    public async Task<StockMovement?> ProcessBarcodeReceiveAsync(string barcode, int quantity, string? DocumentNumber = null, string? notes = null)
    {
        var product = await _productService.GetProductByBarcodeAsync(barcode);
        if (product == null)
            throw new InvalidOperationException($"Barkod '{barcode}' ile ürün bulunamadı.");

        return await AddStockAsync(product.Id, quantity, DocumentNumber, notes, "Barkod Giriş");
    }

    public async Task<StockMovement> AddStockAsync(int productId, int quantity, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            throw new InvalidOperationException($"ID '{productId}' ile ürün bulunamadı.");

        var previousStock = product.Stock;
        var newStock = previousStock + quantity;

        // Stok güncelle
        product.Stock = newStock;
        product.ModifiedDate = DateTime.UtcNow;

        // Stok hareket kaydı oluştur (maliyet bilgisi olmadan basit giriş)
        var stockMovement = new StockMovement
        {
            ProductId = productId,
            MovementType = "IN",
            Quantity = quantity,
            PreviousStock = previousStock,
            NewStock = newStock,
            Notes = notes,
            DocumentNumber = DocumentNumber,
            ProcessedBy = ProcessedBy,
            Date = DateTime.UtcNow
        };

        _context.StockMovements.Add(stockMovement);
        await _context.SaveChangesAsync();

        return stockMovement;
    }

    /// <summary>
    /// Maliyetli stok girişi (ağırlıklı ortalama). UnitCost parametresi zorunlu.
    /// </summary>
    public async Task<StockMovement> AddStockAsync(int productId, int quantity, decimal unitCost, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            throw new InvalidOperationException($"ID '{productId}' ile ürün bulunamadı.");

        if (quantity <= 0)
            throw new InvalidOperationException("Giriş miktarı pozitif olmalıdır.");

        var previousStock = product.Stock;
        var newStock = previousStock + quantity;

        // Ağırlıklı ortalama maliyet (PurchasePrice sahasında tutuyoruz)
        // WA = (PrevQty*PrevCost + InQty*InCost) / (PrevQty+InQty)
        var prevQty = previousStock;
        var prevCost = product.PurchasePrice;
        var inQty = quantity;
        var inCost = unitCost;
        var denom = prevQty + inQty;
        var newAvgCost = denom == 0 ? inCost : ((prevQty * prevCost) + (inQty * inCost)) / denom;

        // Ürünü güncelle
        product.Stock = newStock;
        product.PurchasePrice = Math.Round(newAvgCost, 4);
        product.ModifiedDate = DateTime.UtcNow;

        // Hareket kaydı (UnitCost ve TotalCost set)
        var stockMovement = new StockMovement
        {
            ProductId = productId,
            MovementType = "IN",
            Quantity = quantity,
            PreviousStock = previousStock,
            NewStock = newStock,
            Notes = notes,
            DocumentNumber = DocumentNumber,
            ProcessedBy = ProcessedBy,
            Date = DateTime.UtcNow,
            UnitCost = inCost,
            TotalCost = inCost * quantity
        };

        _context.StockMovements.Add(stockMovement);
        await _context.SaveChangesAsync();

        return stockMovement;
    }

    /// <summary>
    /// Lot ile stok girişi. WA maliyet günceller ve lot bakiyesini oluşturur.
    /// </summary>
    public async Task<StockMovement> AddStockWithLotAsync(int productId, int quantity, decimal unitCost, string lotNumber, DateTime? expiryDate = null, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null)
    {
        if (string.IsNullOrWhiteSpace(lotNumber)) throw new InvalidOperationException("Lot numarası zorunludur.");
        var movement = await AddStockAsync(productId, quantity, unitCost, DocumentNumber, notes, ProcessedBy);

        // Lot oluştur
        var lot = new InventoryLot
        {
            ProductId = productId,
            LotNumber = lotNumber.Trim(),
            ExpiryDate = expiryDate,
            ReceivedQty = quantity,
            RemainingQty = quantity,
            Status = expiryDate.HasValue && expiryDate.Value.Date < DateTime.UtcNow.Date ? LotStatus.Expired : LotStatus.Open,
            CreatedDate = DateTime.UtcNow
        };
        _context.InventoryLots.Add(lot);
        await _context.SaveChangesAsync();
        return movement;
    }

    /// <summary>
    /// FEFO ile stok çıkışı: en erken SKT'li açık lotlardan tahsis eder; eşitse oluşturulma tarihine göre FIFO.
    /// SKT geçmiş lotlar bloklanır.
    /// </summary>
    public async Task<StockMovement> RemoveStockFefoAsync(int productId, int quantity, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null)
    {
        if (quantity <= 0) throw new InvalidOperationException("Çıkış miktarı pozitif olmalıdır.");

        // Expired lotları işaretle
        var today = DateTime.UtcNow.Date;
        var expiredLots = await _context.InventoryLots
            .Where(l => l.ProductId == productId && l.Status == LotStatus.Open && l.ExpiryDate != null && l.ExpiryDate.Value.Date < today)
            .ToListAsync();
        foreach (var l in expiredLots) l.Status = LotStatus.Expired;
        if (expiredLots.Count > 0) await _context.SaveChangesAsync();

        // FEFO seçim: ExpiryDate ASC (null'lar en sona), sonra CreatedDate ASC
        var openLots = await _context.InventoryLots
            .Where(l => l.ProductId == productId && l.Status == LotStatus.Open && l.RemainingQty > 0)
            .OrderBy(l => l.ExpiryDate == null)
            .ThenBy(l => l.ExpiryDate)
            .ThenBy(l => l.CreatedDate)
            .ToListAsync();

        var remaining = quantity;
        foreach (var lot in openLots)
        {
            if (remaining <= 0) break;
            var take = (int)Math.Min(remaining, lot.RemainingQty);
            lot.RemainingQty -= take;
            if (lot.RemainingQty <= 0) { lot.Status = LotStatus.Closed; lot.ClosedDate = DateTime.UtcNow; }
            remaining -= take;
        }

        if (remaining > 0)
            throw new InvalidOperationException("Yetersiz lot bakiyesi. FEFO tahsis yapılamadı.");

        // Ürün stok ve COGS kaydı standart OUT ile yapılır
        var movement = await RemoveStockAsync(productId, quantity, DocumentNumber, notes, ProcessedBy);
        await _context.SaveChangesAsync();
        return movement;
    }

    public async Task<StockMovement> RemoveStockAsync(int productId, int quantity, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            throw new InvalidOperationException($"ID '{productId}' ile ürün bulunamadı.");

        if (product.Stock < quantity)
            throw new InvalidOperationException($"Yetersiz stok. Mevcut: {product.Stock}, İstenen: {quantity}");

        var previousStock = product.Stock;
        var newStock = previousStock - quantity;

        // Stok güncelle
        product.Stock = newStock;
        product.ModifiedDate = DateTime.UtcNow;

        // Stok hareket kaydı oluştur (COGS için UnitCost = o anki WA cost, TotalCost = Quantity * WA)
        var stockMovement = new StockMovement
        {
            ProductId = productId,
            MovementType = "OUT",
            Quantity = quantity,
            PreviousStock = previousStock,
            NewStock = newStock,
            Notes = notes,
            DocumentNumber = DocumentNumber,
            ProcessedBy = ProcessedBy,
            Date = DateTime.UtcNow,
            UnitCost = product.PurchasePrice,
            TotalCost = product.PurchasePrice * quantity
        };

        _context.StockMovements.Add(stockMovement);
        await _context.SaveChangesAsync();

        return stockMovement;
    }

    public async Task<StockMovement> AdjustStockAsync(int productId, int newQuantity, string? notes = null, string? ProcessedBy = null)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            throw new InvalidOperationException($"ID '{productId}' ile ürün bulunamadı.");

        var previousStock = product.Stock;
        var difference = newQuantity - previousStock;

        // Stok güncelle
        product.Stock = newQuantity;
        product.ModifiedDate = DateTime.UtcNow;

        // Stok hareket kaydı oluştur
        var stockMovement = new StockMovement
        {
            ProductId = productId,
            MovementType = "ADJUSTMENT",
            Quantity = Math.Abs(difference),
            PreviousStock = previousStock,
            NewStock = newQuantity,
            Notes = notes ?? $"Stok düzeltmesi: {previousStock} → {newQuantity}",
            ProcessedBy = ProcessedBy,
            Date = DateTime.UtcNow
        };

        _context.StockMovements.Add(stockMovement);
        await _context.SaveChangesAsync();

        return stockMovement;
    }

    public async Task<int> GetCurrentStockAsync(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        return product?.Stock ?? 0;
    }

    public async Task<int> GetCurrentStockByBarcodeAsync(string barcode)
    {
        var product = await _productService.GetProductByBarcodeAsync(barcode);
        return product?.Stock ?? 0;
    }

    public async Task<IEnumerable<StockMovement>> GetStockMovementsAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.Date >= fromDate && sm.Date <= toDate)
            .OrderByDescending(sm => sm.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockMovement>> GetProductStockMovementsAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.ProductId == productId);

        if (fromDate != null)
            query = query.Where(sm => sm.Date >= fromDate.Value);

        if (toDate != null)
            query = query.Where(sm => sm.Date <= toDate.Value);

        return await query
            .OrderByDescending(sm => sm.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetCriticalStockProductsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
            .OrderBy(p => p.Stock)
            .ToListAsync();
    }

    public async Task<decimal> CalculateInventoryValueAsync(bool useCostPrice = true)
    {
        if (useCostPrice)
        {
            return await _context.Products
                .Where(p => p.IsActive && p.PurchasePrice > 0)
                .SumAsync(p => p.Stock * p.PurchasePrice);
        }
        else
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .SumAsync(p => p.Stock * p.SalePrice);
        }
    }

    public async Task<decimal> CalculateCategoryInventoryValueAsync(string category, bool useCostPrice = true)
    {
        if (useCostPrice)
        {
            return await _context.Products
                .Where(p => p.IsActive && p.Category.Name == category && p.PurchasePrice > 0)
                .SumAsync(p => p.Stock * p.PurchasePrice);
        }
        else
        {
            return await _context.Products
                .Where(p => p.IsActive && p.Category.Name == category)
                .SumAsync(p => p.Stock * p.SalePrice);
        }
    }

    public async Task<IEnumerable<StockMovement>> GetMovementsByTypeAsync(StockMovementType movementType, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.MovementType == movementType.ToString());

        if (fromDate != null)
            query = query.Where(sm => sm.Date >= fromDate.Value);

        if (toDate != null)
            query = query.Where(sm => sm.Date <= toDate.Value);

        return await query
            .OrderByDescending(sm => sm.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockMovement>> GetRecentMovementsAsync(int days = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        return await _context.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.Date >= cutoffDate)
            .OrderByDescending(sm => sm.Date)
            .Take(100) // Son 100 hareket
            .ToListAsync();
    }

    public async Task<InventoryReport> GenerateInventoryReportAsync(DateTime? asOfDate = null)
    {
        var reportDate = asOfDate ?? DateTime.UtcNow;

        var products = await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        var totalProducts = products.Count;
        var activeProducts = products.Count(p => p.IsActive);
        var lowStockProducts = products.Count(p => p.Stock <= p.MinimumStock);
        var outOfStockProducts = products.Count(p => p.Stock <= 0);

        var totalInventoryValue = products.Sum(p => p.Stock * p.SalePrice);
        var totalCostValue = products.Where(p => p.PurchasePrice > 0).Sum(p => p.Stock * p.PurchasePrice);

        var productSummaries = products.Select(p => new ProductStockSummary
        {
            ProductId = p.Id,
            ProductName = p.Name,
            Sku = p.SKU,
            Barcode = p.Barcode,
            CurrentStock = p.Stock,
            MinStockLevel = p.MinimumStock,
            UnitPrice = p.SalePrice,
            UnitCost = p.PurchasePrice,
            TotalValue = p.Stock * p.SalePrice
        }).ToList();

        return new InventoryReport
        {
            ReportDate = reportDate,
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            LowStockProducts = lowStockProducts,
            OutOfStockProducts = outOfStockProducts,
            TotalInventoryValue = totalInventoryValue,
            TotalCostValue = totalCostValue,
            ProductSummaries = productSummaries
        };
    }

    public async Task<bool> CancelStockMovementAsync(int movementId, string? reason = null)
    {
        var movement = await _context.StockMovements
            .Include(sm => sm.Product)
            .FirstOrDefaultAsync(sm => sm.Id == movementId);

        if (movement == null)
            return false;

        // Sadece bugün yapılan hareketler iptal edilebilir
        if (movement.Date.Date != DateTime.UtcNow.Date)
            throw new InvalidOperationException("Sadece bugün yapılan hareketler iptal edilebilir.");

        var product = movement.Product;

        // Ters hareket oluştur
        var reverseMovement = new StockMovement
        {
            ProductId = movement.ProductId,
            MovementType = movement.MovementType == "IN" ? "OUT" : "IN",
            Quantity = movement.Quantity,
            PreviousStock = product.Stock,
            NewStock = movement.PreviousStock,
            Notes = $"İptal: {movement.Notes} - {reason}",
            DocumentNumber = movement.DocumentNumber,
            Date = DateTime.UtcNow
        };

        // Ürün stokunu eski haline getir
        product.Stock = movement.PreviousStock;
        product.ModifiedDate = DateTime.UtcNow;

        _context.StockMovements.Add(reverseMovement);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<StockMovement>> BulkStockUpdateAsync(IEnumerable<BulkStockUpdate> updates, string? ProcessedBy = null)
    {
        var movements = new List<StockMovement>();

        foreach (var update in updates)
        {
            try
            {
                StockMovement movement;

                switch (update.MovementType)
                {
                    case StockMovementType.StockIn:
                        movement = await AddStockAsync(update.ProductId, update.NewQuantity, update.DocumentNumber, update.Notes, ProcessedBy);
                        break;
                    case StockMovementType.StockOut:
                        movement = await RemoveStockAsync(update.ProductId, update.NewQuantity, update.DocumentNumber, update.Notes, ProcessedBy);
                        break;
                    case StockMovementType.Adjustment:
                        movement = await AdjustStockAsync(update.ProductId, update.NewQuantity, update.Notes, ProcessedBy);
                        break;
                    default:
                        continue;
                }

                movements.Add(movement);
            }
            catch (Exception)
            {
                // Hata olan güncellemeleri atla, devam et
                continue;
            }
        }

        return movements;
    }
}
