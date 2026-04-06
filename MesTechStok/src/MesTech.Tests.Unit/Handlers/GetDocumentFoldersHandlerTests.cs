using System.Collections.ObjectModel;
using FluentAssertions;
using MesTech.Application.Features.Documents.Queries.GetDocumentFolders;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetDocumentFoldersHandlerTests
{
    private readonly Mock<IDocumentFolderRepository> _folderRepoMock = new();
    private readonly Mock<IDocumentRepository> _docRepoMock = new();
    private readonly Mock<ILogger<GetDocumentFoldersHandler>> _loggerMock = new();
    private readonly GetDocumentFoldersHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public GetDocumentFoldersHandlerTests()
    {
        _sut = new GetDocumentFoldersHandler(
            _folderRepoMock.Object,
            _docRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithFolders_ReturnsFolderDtosWithDocumentCounts()
    {
        // Arrange
        var folder1 = DocumentFolder.Create(TenantId, "Invoices");
        var folder2 = DocumentFolder.Create(TenantId, "Contracts");

        var doc1 = Document.Create(TenantId, "inv-001.pdf", "inv-001.pdf", "application/pdf",
            1024, "/storage/inv-001.pdf", UserId, folderId: folder1.Id);
        var doc2 = Document.Create(TenantId, "inv-002.pdf", "inv-002.pdf", "application/pdf",
            2048, "/storage/inv-002.pdf", UserId, folderId: folder1.Id);

        _folderRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentFolder> { folder1, folder2 });
        _docRepoMock.Setup(r => r.CountByFolderIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyDictionary<Guid, int>(new Dictionary<Guid, int>
            {
                { folder1.Id, 2 },
                { folder2.Id, 0 }
            }));

        var query = new GetDocumentFoldersQuery(TenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Folders.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        var invoicesDto = result.Folders.First(f => f.Name == "Invoices");
        invoicesDto.DocumentCount.Should().Be(2);

        var contractsDto = result.Folders.First(f => f.Name == "Contracts");
        contractsDto.DocumentCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoFolders_ReturnsEmptyResult()
    {
        // Arrange
        _folderRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentFolder>());
        _docRepoMock.Setup(r => r.CountByFolderIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyDictionary<Guid, int>(new Dictionary<Guid, int>()));

        var query = new GetDocumentFoldersQuery(TenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Folders.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_FolderWithParent_MapsParentIdCorrectly()
    {
        // Arrange
        var parentFolder = DocumentFolder.Create(TenantId, "Root");
        var childFolder = DocumentFolder.Create(TenantId, "SubFolder", parentFolderId: parentFolder.Id);

        _folderRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentFolder> { parentFolder, childFolder });
        _docRepoMock.Setup(r => r.CountByFolderIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyDictionary<Guid, int>(new Dictionary<Guid, int>()));

        var query = new GetDocumentFoldersQuery(TenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var rootDto = result.Folders.First(f => f.Name == "Root");
        rootDto.ParentId.Should().BeNull();

        var childDto = result.Folders.First(f => f.Name == "SubFolder");
        childDto.ParentId.Should().Be(parentFolder.Id);
    }

    [Fact]
    public async Task Handle_CallsGetByFolderForEachFolder()
    {
        // Arrange — 3 folders, verify batch count called once (N+1 → 1+1)
        var folders = Enumerable.Range(1, 3)
            .Select(i => DocumentFolder.Create(TenantId, $"Folder-{i}"))
            .ToList();

        _folderRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folders);
        _docRepoMock.Setup(r => r.CountByFolderIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyDictionary<Guid, int>(new Dictionary<Guid, int>()));

        var query = new GetDocumentFoldersQuery(TenantId);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _docRepoMock.Verify(r => r.CountByFolderIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MapsCreatedAtFromEntity()
    {
        // Arrange
        var folder = DocumentFolder.Create(TenantId, "Archive");

        _folderRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentFolder> { folder });
        _docRepoMock.Setup(r => r.CountByFolderIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyDictionary<Guid, int>(new Dictionary<Guid, int>()));

        var query = new GetDocumentFoldersQuery(TenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Folders.Single();
        dto.Id.Should().Be(folder.Id);
        dto.Name.Should().Be("Archive");
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
