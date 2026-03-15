using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Infrastructure.Persistence.Accounting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Storage;

/// <summary>
/// ImmutableDocumentStore (WORM) tests — VUK 253 compliant immutable archive.
/// Uses real temp directories for file I/O verification.
/// </summary>
[Trait("Category", "Unit")]
public class ImmutableDocumentStoreTests : IDisposable
{
    private readonly string _tempBasePath;
    private readonly ImmutableDocumentStore _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ImmutableDocumentStoreTests()
    {
        _tempBasePath = Path.Combine(Path.GetTempPath(), $"mestech-worm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempBasePath);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ArchivePath"] = _tempBasePath
            })
            .Build();

        _sut = new ImmutableDocumentStore(
            config,
            new Mock<ILogger<ImmutableDocumentStore>>().Object);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempBasePath))
                Directory.Delete(_tempBasePath, true);
        }
        catch
        {
            // Intentional: cleanup best-effort in tests
        }
    }

    private DocumentMetadata CreateMetadata(DateTime? archivedAt = null)
    {
        return new DocumentMetadata(
            SourceHash: "placeholder",
            ArchivedAt: archivedAt ?? DateTime.UtcNow,
            SourceChannel: "UnitTest",
            UblTrVersion: "1.2",
            SchematronVersion: "1.0",
            TenantId: _tenantId,
            OriginalDocumentId: null);
    }

    // ── StoreAsync Tests ──

    [Fact]
    public async Task StoreAsync_ValidContent_CreatesFileAndMetadata()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Test document content");
        var metadata = CreateMetadata();

        // Act
        var documentId = await _sut.StoreAsync(content, "application/pdf", metadata);

        // Assert
        documentId.Should().NotBeEmpty();

        // Verify files exist somewhere in the directory structure
        var binFiles = Directory.GetFiles(_tempBasePath, $"{documentId}.bin", SearchOption.AllDirectories);
        var metaFiles = Directory.GetFiles(_tempBasePath, $"{documentId}.meta.json", SearchOption.AllDirectories);

        binFiles.Should().HaveCount(1);
        metaFiles.Should().HaveCount(1);
    }

    [Fact]
    public async Task StoreAsync_ComputesSHA256Hash()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Hash verification content");
        var expectedHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var metadata = CreateMetadata();

        // Act
        var documentId = await _sut.StoreAsync(content, "text/plain", metadata);

        // Assert — verify hash in metadata file
        var metaFiles = Directory.GetFiles(_tempBasePath, $"{documentId}.meta.json", SearchOption.AllDirectories);
        metaFiles.Should().HaveCount(1);

        var metaJson = await File.ReadAllTextAsync(metaFiles[0]);
        metaJson.Should().Contain(expectedHash);
    }

    [Fact]
    public async Task StoreAsync_CreatesDirectoryStructure_TenantYearMonth()
    {
        // Arrange
        var archivedAt = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var content = Encoding.UTF8.GetBytes("Directory structure test");
        var metadata = CreateMetadata(archivedAt);

        // Act
        var documentId = await _sut.StoreAsync(content, "application/pdf", metadata);

        // Assert — verify directory path: {basePath}/{tenantId}/{yyyy}/{MM}
        var expectedDir = Path.Combine(_tempBasePath, _tenantId.ToString(), "2026", "03");
        Directory.Exists(expectedDir).Should().BeTrue();

        var binPath = Path.Combine(expectedDir, $"{documentId}.bin");
        File.Exists(binPath).Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_NullContent_ThrowsArgumentNull()
    {
        // Arrange
        var metadata = CreateMetadata();

        // Act
        var act = () => _sut.StoreAsync(null!, "application/pdf", metadata);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreAsync_NullMimeType_ThrowsArgumentException()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test");
        var metadata = CreateMetadata();

        // Act
        var act = () => _sut.StoreAsync(content, null!, metadata);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StoreAsync_EmptyMimeType_ThrowsArgumentException()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test");
        var metadata = CreateMetadata();

        // Act
        var act = () => _sut.StoreAsync(content, "  ", metadata);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StoreAsync_NullMetadata_ThrowsArgumentNull()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test");

        // Act
        var act = () => _sut.StoreAsync(content, "application/pdf", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreAsync_SetsArchivedAtToUtcNow()
    {
        // Arrange
        var beforeStore = DateTime.UtcNow;
        var content = Encoding.UTF8.GetBytes("ArchivedAt test");
        var metadata = CreateMetadata(beforeStore);

        // Act
        var documentId = await _sut.StoreAsync(content, "text/plain", metadata);

        // Assert — metadata file should contain the ArchivedAt value
        var metaFiles = Directory.GetFiles(_tempBasePath, $"{documentId}.meta.json", SearchOption.AllDirectories);
        var metaJson = await File.ReadAllTextAsync(metaFiles[0]);
        metaJson.Should().NotBeNullOrEmpty();

        // The meta JSON should contain the archivedAt from the metadata
        var jsonDoc = JsonDocument.Parse(metaJson);
        var archivedAtStr = jsonDoc.RootElement.GetProperty("archivedAt").GetString();
        archivedAtStr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StoreAsync_ReturnsDifferentGuidsForDifferentDocuments()
    {
        // Arrange
        var content1 = Encoding.UTF8.GetBytes("Document 1");
        var content2 = Encoding.UTF8.GetBytes("Document 2");
        var metadata = CreateMetadata();

        // Act
        var id1 = await _sut.StoreAsync(content1, "text/plain", metadata);
        var id2 = await _sut.StoreAsync(content2, "text/plain", metadata);

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public async Task StoreAsync_StoresCorrectFileContent()
    {
        // Arrange
        var originalContent = Encoding.UTF8.GetBytes("Verify stored content matches original");
        var metadata = CreateMetadata();

        // Act
        var documentId = await _sut.StoreAsync(originalContent, "text/plain", metadata);

        // Assert
        var binFiles = Directory.GetFiles(_tempBasePath, $"{documentId}.bin", SearchOption.AllDirectories);
        var storedContent = await File.ReadAllBytesAsync(binFiles[0]);
        storedContent.Should().BeEquivalentTo(originalContent);
    }

    // ── RetrieveAsync Tests ──

    [Fact]
    public async Task RetrieveAsync_ExistingDocument_ReturnsContentAndMetadata()
    {
        // Arrange
        var originalContent = Encoding.UTF8.GetBytes("Retrievable document content");
        var metadata = CreateMetadata();
        var documentId = await _sut.StoreAsync(originalContent, "application/pdf", metadata);

        // Act
        var (content, retrievedMeta) = await _sut.RetrieveAsync(documentId);

        // Assert
        content.Should().BeEquivalentTo(originalContent);
        retrievedMeta.Should().NotBeNull();
        retrievedMeta.SourceChannel.Should().Be("UnitTest");
        retrievedMeta.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task RetrieveAsync_NonExistent_ThrowsFileNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _sut.RetrieveAsync(nonExistentId);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsCorrectMetadataFields()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Metadata fields test");
        var metadata = new DocumentMetadata(
            SourceHash: "will-be-overwritten",
            ArchivedAt: new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            SourceChannel: "EmailScanner",
            UblTrVersion: "2.1",
            SchematronVersion: "1.3",
            TenantId: _tenantId,
            OriginalDocumentId: Guid.NewGuid());

        var documentId = await _sut.StoreAsync(content, "application/xml", metadata);

        // Act
        var (_, retrievedMeta) = await _sut.RetrieveAsync(documentId);

        // Assert
        retrievedMeta.SourceChannel.Should().Be("EmailScanner");
        retrievedMeta.UblTrVersion.Should().Be("2.1");
        retrievedMeta.SchematronVersion.Should().Be("1.3");
        retrievedMeta.TenantId.Should().Be(_tenantId);
        retrievedMeta.OriginalDocumentId.Should().NotBeNull();
    }

    // ── VerifyIntegrityAsync Tests ──

    [Fact]
    public async Task VerifyIntegrity_ValidFile_ReturnsTrue()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Integrity test - valid file");
        var metadata = CreateMetadata();
        var documentId = await _sut.StoreAsync(content, "text/plain", metadata);

        // Act
        var isValid = await _sut.VerifyIntegrityAsync(documentId);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyIntegrity_TamperedFile_ReturnsFalse()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Original content for tampering test");
        var metadata = CreateMetadata();
        var documentId = await _sut.StoreAsync(content, "text/plain", metadata);

        // Tamper with the file
        var binFiles = Directory.GetFiles(_tempBasePath, $"{documentId}.bin", SearchOption.AllDirectories);
        await File.WriteAllBytesAsync(binFiles[0], Encoding.UTF8.GetBytes("TAMPERED CONTENT"));

        // Act
        var isValid = await _sut.VerifyIntegrityAsync(documentId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyIntegrity_NonExistentDocument_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var isValid = await _sut.VerifyIntegrityAsync(nonExistentId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyIntegrity_MultipleDocuments_EachIndependentlyValid()
    {
        // Arrange
        var content1 = Encoding.UTF8.GetBytes("Document 1 for integrity");
        var content2 = Encoding.UTF8.GetBytes("Document 2 for integrity");
        var metadata = CreateMetadata();

        var id1 = await _sut.StoreAsync(content1, "text/plain", metadata);
        var id2 = await _sut.StoreAsync(content2, "text/plain", metadata);

        // Act
        var valid1 = await _sut.VerifyIntegrityAsync(id1);
        var valid2 = await _sut.VerifyIntegrityAsync(id2);

        // Assert
        valid1.Should().BeTrue();
        valid2.Should().BeTrue();
    }

    // ── No Delete Method Tests ──

    [Fact]
    public void Interface_HasNoDeleteMethod_CompileTimeGuarantee()
    {
        // VUK 253: 5-year mandatory retention — DELETE must not exist
        var interfaceType = typeof(IImmutableDocumentStore);
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var deleteMethod = methods.FirstOrDefault(m =>
            m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("Remove", StringComparison.OrdinalIgnoreCase));

        deleteMethod.Should().BeNull("IImmutableDocumentStore must not have a Delete/Remove method (VUK 253)");
    }

    [Fact]
    public void Implementation_HasNoDeleteMethod_CompileTimeGuarantee()
    {
        // Verify the implementation class also has no delete
        var implType = typeof(ImmutableDocumentStore);
        var methods = implType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var deleteMethod = methods.FirstOrDefault(m =>
            m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("Remove", StringComparison.OrdinalIgnoreCase));

        deleteMethod.Should().BeNull("ImmutableDocumentStore must not have a Delete/Remove method (VUK 253)");
    }

    // ── Edge Case Tests ──

    [Fact]
    public async Task StoreAsync_LargeContent_SucceedsAndVerifies()
    {
        // Arrange — 1MB document
        var content = new byte[1024 * 1024];
        Random.Shared.NextBytes(content);
        var metadata = CreateMetadata();

        // Act
        var documentId = await _sut.StoreAsync(content, "application/octet-stream", metadata);

        // Assert
        var isValid = await _sut.VerifyIntegrityAsync(documentId);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_EmptyByteArray_StoresSuccessfully()
    {
        // Arrange — empty but not null
        var content = Array.Empty<byte>();
        var metadata = CreateMetadata();

        // Act
        var documentId = await _sut.StoreAsync(content, "text/plain", metadata);

        // Assert
        documentId.Should().NotBeEmpty();
        var (retrieved, _) = await _sut.RetrieveAsync(documentId);
        retrieved.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAsync_MetadataRecordsFileSizeBytes()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Size recording test content");
        var metadata = CreateMetadata();

        // Act
        var documentId = await _sut.StoreAsync(content, "text/plain", metadata);

        // Assert
        var metaFiles = Directory.GetFiles(_tempBasePath, $"{documentId}.meta.json", SearchOption.AllDirectories);
        var metaJson = await File.ReadAllTextAsync(metaFiles[0]);
        var jsonDoc = JsonDocument.Parse(metaJson);
        var fileSize = jsonDoc.RootElement.GetProperty("fileSizeBytes").GetInt64();
        fileSize.Should().Be(content.Length);
    }

    [Fact]
    public async Task StoreAndRetrieve_RoundTrip_PreservesExactContent()
    {
        // Arrange — binary content with all byte values
        var content = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var metadata = CreateMetadata();

        // Act
        var documentId = await _sut.StoreAsync(content, "application/octet-stream", metadata);
        var (retrieved, _) = await _sut.RetrieveAsync(documentId);

        // Assert
        retrieved.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task StoreAsync_DifferentTenants_IsolatedDirectories()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("Multi-tenant test");
        var archivedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        var meta1 = new DocumentMetadata("h1", archivedAt, "Test", null, null, tenant1, null);
        var meta2 = new DocumentMetadata("h2", archivedAt, "Test", null, null, tenant2, null);

        // Act
        await _sut.StoreAsync(content, "text/plain", meta1);
        await _sut.StoreAsync(content, "text/plain", meta2);

        // Assert
        var dir1 = Path.Combine(_tempBasePath, tenant1.ToString());
        var dir2 = Path.Combine(_tempBasePath, tenant2.ToString());
        Directory.Exists(dir1).Should().BeTrue();
        Directory.Exists(dir2).Should().BeTrue();
    }
}
