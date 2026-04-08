using FluentAssertions;
using MesTech.Application.Features.Documents.Queries.GetDocuments;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Documents.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetDocumentsHandlerTests
{
    private readonly Mock<IDocumentRepository> _docRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetDocumentsHandler CreateSut() => new(_docRepo.Object);

    [Fact]
    public void Constructor_NullRepo_Throws()
    {
        var act = () => new GetDocumentsHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("docRepo");
    }

    [Fact]
    public async Task Handle_NullFolderId_ReturnsEmptyResult()
    {
        var query = new GetDocumentsQuery(_tenantId, FolderId: null);

        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        _docRepo.Verify(r => r.GetByFolderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFolderId_ReturnsDocumentsOrderedByCreatedAtDesc()
    {
        var folderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var docs = new List<Document>
        {
            Document.Create(_tenantId, "old.pdf", "old.pdf", "application/pdf", 1024, "/store/old.pdf", userId, folderId),
            Document.Create(_tenantId, "new.pdf", "new.pdf", "application/pdf", 2048, "/store/new.pdf", userId, folderId)
        };
        // Set different CreatedAt to test ordering
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("CreatedAt")!.SetValue(docs[0], DateTime.UtcNow.AddDays(-2));
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("CreatedAt")!.SetValue(docs[1], DateTime.UtcNow);

        _docRepo.Setup(r => r.GetByFolderAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);

        var query = new GetDocumentsQuery(_tenantId, FolderId: folderId, Page: 1, PageSize: 50);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Documents.Should().HaveCount(2);
        result.Documents[0].FileName.Should().Be("new.pdf");
        result.Documents[1].FileName.Should().Be("old.pdf");
    }

    [Fact]
    public async Task Handle_PaginationApplied_ReturnsCorrectPage()
    {
        var folderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var docs = Enumerable.Range(1, 10).Select(i =>
            Document.Create(_tenantId, $"doc{i}.pdf", $"doc{i}.pdf", "application/pdf", 100 * i, $"/store/doc{i}.pdf", userId, folderId)
        ).ToList();

        _docRepo.Setup(r => r.GetByFolderAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);

        var query = new GetDocumentsQuery(_tenantId, FolderId: folderId, Page: 2, PageSize: 3);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(10);
        result.Documents.Should().HaveCount(3);
    }
}
