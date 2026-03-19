using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Toplu ürün içe/dışa aktarma servis arayüzü.
/// Excel dosyalarından ürün validasyonu, import ve export işlemleri.
/// </summary>
public interface IBulkProductImportService
{
    /// <summary>
    /// Excel dosyasını doğrular, satır bazlı hata raporu döner.
    /// </summary>
    Task<ImportValidationResult> ValidateExcelAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Excel dosyasından ürünleri toplu olarak içe aktarır.
    /// Batch processing: 100 satır per SaveChanges.
    /// Performance hedefi: 10K satır &lt; 30 saniye.
    /// </summary>
    Task<ImportResult> ImportProductsAsync(
        Stream fileStream,
        ImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ürünleri Excel formatında dışa aktarır.
    /// </summary>
    Task<byte[]> ExportProductsAsync(
        BulkExportOptions options,
        CancellationToken cancellationToken = default);
}

// ── DTOs ──

public record ImportValidationResult(
    bool IsValid,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    List<ImportRowError> Errors);

public record ImportRowError(
    int RowNumber,
    string Field,
    string Message);

public record ImportResult(
    ImportStatus Status,
    int TotalRows,
    int ImportedCount,
    int UpdatedCount,
    int SkippedCount,
    int ErrorCount,
    List<ImportRowError> Errors,
    TimeSpan Duration);

public record ImportOptions(
    bool UpdateExisting = false,
    bool SkipErrors = true,
    PlatformType? TargetPlatform = null,
    Guid? CategoryId = null);

public record BulkExportOptions(
    PlatformType? Platform = null,
    Guid? CategoryId = null,
    bool? InStock = null,
    string Format = "xlsx");
