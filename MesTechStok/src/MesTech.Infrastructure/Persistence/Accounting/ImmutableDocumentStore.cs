using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Persistence.Accounting;

/// <summary>
/// VUK 253 uyumlu dosya tabanli degistirilemez belge deposu.
/// Dosyalar: {basePath}/{tenantId}/{yyyy}/{MM}/{documentId}.bin
/// Meta: {documentId}.meta.json
/// SHA-256 hash ile butunluk garantisi. DELETE metodu YOKTUR.
/// </summary>
public class ImmutableDocumentStore : IImmutableDocumentStore
{
    private readonly string _basePath;
    private readonly ILogger<ImmutableDocumentStore> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ImmutableDocumentStore(IConfiguration configuration, ILogger<ImmutableDocumentStore> logger)
    {
        _basePath = configuration["Storage:ArchivePath"] ?? "./data/archive";
        _logger = logger;
    }

    public async Task<Guid> StoreAsync(byte[] content, string mimeType, DocumentMetadata metadata, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentNullException.ThrowIfNull(metadata);

        var documentId = Guid.NewGuid();
        var hash = ComputeSha256(content);

        var directory = BuildDirectory(metadata.TenantId, metadata.ArchivedAt);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{documentId}.bin");
        var metaPath = Path.Combine(directory, $"{documentId}.meta.json");

        // Store file content
        await File.WriteAllBytesAsync(filePath, content, ct);

        // Store metadata with computed hash
        var storedMeta = new StoredMetadata
        {
            DocumentId = documentId,
            SourceHash = hash,
            MimeType = mimeType,
            ArchivedAt = metadata.ArchivedAt,
            SourceChannel = metadata.SourceChannel,
            UblTrVersion = metadata.UblTrVersion,
            SchematronVersion = metadata.SchematronVersion,
            TenantId = metadata.TenantId,
            OriginalDocumentId = metadata.OriginalDocumentId,
            FileSizeBytes = content.Length
        };

        var metaJson = JsonSerializer.Serialize(storedMeta, JsonOptions);
        await File.WriteAllTextAsync(metaPath, metaJson, ct);

        _logger.LogInformation(
            "Document archived: {DocumentId}, Tenant: {TenantId}, Hash: {Hash}, Size: {Size} bytes",
            documentId, metadata.TenantId, hash, content.Length);

        return documentId;
    }

    public async Task<(byte[] Content, DocumentMetadata Metadata)> RetrieveAsync(Guid documentId, CancellationToken ct = default)
    {
        var (filePath, metaPath) = await FindDocumentPathsAsync(documentId, ct);

        if (filePath == null || metaPath == null)
            throw new FileNotFoundException($"Archived document not found: {documentId}");

        var content = await File.ReadAllBytesAsync(filePath, ct);
        var metaJson = await File.ReadAllTextAsync(metaPath, ct);
        var storedMeta = JsonSerializer.Deserialize<StoredMetadata>(metaJson, JsonOptions)
            ?? throw new InvalidOperationException($"Corrupted metadata for document: {documentId}");

        var metadata = new DocumentMetadata(
            storedMeta.SourceHash,
            storedMeta.ArchivedAt,
            storedMeta.SourceChannel,
            storedMeta.UblTrVersion,
            storedMeta.SchematronVersion,
            storedMeta.TenantId,
            storedMeta.OriginalDocumentId);

        return (content, metadata);
    }

    public async Task<bool> VerifyIntegrityAsync(Guid documentId, CancellationToken ct = default)
    {
        var (filePath, metaPath) = await FindDocumentPathsAsync(documentId, ct);

        if (filePath == null || metaPath == null)
        {
            _logger.LogWarning("Integrity check failed — document not found: {DocumentId}", documentId);
            return false;
        }

        var content = await File.ReadAllBytesAsync(filePath, ct);
        var metaJson = await File.ReadAllTextAsync(metaPath, ct);
        var storedMeta = JsonSerializer.Deserialize<StoredMetadata>(metaJson, JsonOptions);

        if (storedMeta == null)
        {
            _logger.LogWarning("Integrity check failed — corrupted metadata: {DocumentId}", documentId);
            return false;
        }

        var currentHash = ComputeSha256(content);
        var isValid = string.Equals(currentHash, storedMeta.SourceHash, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
        {
            _logger.LogError(
                "INTEGRITY VIOLATION — Document {DocumentId}: stored hash {StoredHash} != computed hash {ComputedHash}",
                documentId, storedMeta.SourceHash, currentHash);
        }

        return isValid;
    }

    // NO delete method — VUK 253: 5-year mandatory retention (compile-time guarantee)

    private string BuildDirectory(Guid tenantId, DateTime archivedAt)
    {
        return Path.Combine(
            _basePath,
            tenantId.ToString(),
            archivedAt.ToString("yyyy"),
            archivedAt.ToString("MM"));
    }

    private Task<(string? FilePath, string? MetaPath)> FindDocumentPathsAsync(Guid documentId, CancellationToken ct)
    {
        // Search across all tenant/year/month directories for the document
        var fileName = $"{documentId}.bin";
        var metaName = $"{documentId}.meta.json";

        if (!Directory.Exists(_basePath))
            return Task.FromResult<(string?, string?)>((null, null));

        foreach (var tenantDir in Directory.GetDirectories(_basePath))
        {
            foreach (var yearDir in Directory.GetDirectories(tenantDir))
            {
                foreach (var monthDir in Directory.GetDirectories(yearDir))
                {
                    ct.ThrowIfCancellationRequested();

                    var filePath = Path.Combine(monthDir, fileName);
                    var metaPath = Path.Combine(monthDir, metaName);

                    if (File.Exists(filePath) && File.Exists(metaPath))
                        return Task.FromResult<(string?, string?)>((filePath, metaPath));
                }
            }
        }

        return Task.FromResult<(string?, string?)>((null, null));
    }

    private static string ComputeSha256(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Dosya ile birlikte saklanan ic meta veri yapisi.
    /// </summary>
    private sealed class StoredMetadata
    {
        public Guid DocumentId { get; set; }
        public string SourceHash { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public DateTime ArchivedAt { get; set; }
        public string SourceChannel { get; set; } = string.Empty;
        public string? UblTrVersion { get; set; }
        public string? SchematronVersion { get; set; }
        public Guid TenantId { get; set; }
        public Guid? OriginalDocumentId { get; set; }
        public long FileSizeBytes { get; set; }
    }
}
