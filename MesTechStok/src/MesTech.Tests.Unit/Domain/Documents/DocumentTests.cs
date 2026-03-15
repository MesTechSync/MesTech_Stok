using FluentAssertions;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Documents;

namespace MesTech.Tests.Unit.Domain.Documents;

/// <summary>
/// Document and DocumentFolder entity domain logic tests — H28 DEV5 T5.3
/// </summary>
public class DocumentTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();

    private static Document CreateDocument(long sizeBytes = 102400)
        => Document.Create(_tenantId, "invoice_001.pdf", "Fatura-Ocak.pdf",
            "application/pdf", sizeBytes, "mestech-documents/2026/03/abc123_invoice.pdf", _userId);

    [Fact]
    public void Create_ValidData_ShouldRaiseDocumentUploadedEvent()
    {
        var doc = CreateDocument();
        doc.DomainEvents.Should().ContainSingle(e => e is DocumentUploadedEvent);
        var ev = (DocumentUploadedEvent)doc.DomainEvents.First();
        ev.FileName.Should().Be("invoice_001.pdf");
        ev.DocumentId.Should().Be(doc.Id);
        ev.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void Create_EmptyFileName_ShouldThrow()
    {
        var act = () => Document.Create(_tenantId, "", "Orijinal.pdf",
            "application/pdf", 1000, "/path/to/file", _userId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhitespaceFileName_ShouldThrow()
    {
        var act = () => Document.Create(_tenantId, "   ", "Orijinal.pdf",
            "application/pdf", 1000, "/path/to/file", _userId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SetsCorrectProperties()
    {
        var doc = CreateDocument(51200);
        doc.FileSizeBytes.Should().Be(51200);
        doc.ContentType.Should().Be("application/pdf");
        doc.OriginalFileName.Should().Be("Fatura-Ocak.pdf");
        doc.UploadedByUserId.Should().Be(_userId);
        doc.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void Create_DefaultVisibility_ShouldBeTenantOnly()
    {
        var doc = CreateDocument();
        doc.Visibility.Should().Be(DocumentVisibility.TenantOnly);
    }

    [Fact]
    public void Create_WithFolderId_ShouldSetFolderId()
    {
        var folderId = Guid.NewGuid();
        var doc = Document.Create(_tenantId, "file.pdf", "file.pdf", "application/pdf",
            1000, "/path", _userId, folderId: folderId);
        doc.FolderId.Should().Be(folderId);
    }
}

public class DocumentFolderTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidName_ShouldSucceed()
    {
        var folder = DocumentFolder.Create(_tenantId, "Kargo Belgeleri");
        folder.Name.Should().Be("Kargo Belgeleri");
        folder.IsSystem.Should().BeFalse();
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        var act = () => DocumentFolder.Create(_tenantId, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_NonSystemFolder_ShouldSucceed()
    {
        var folder = DocumentFolder.Create(_tenantId, "Eski İsim");
        folder.Rename("Yeni İsim");
        folder.Name.Should().Be("Yeni İsim");
    }

    [Fact]
    public void Rename_SystemFolder_ShouldThrow()
    {
        var folder = DocumentFolder.Create(_tenantId, "Faturalar", isSystem: true);
        var act = () => folder.Rename("Değiştirilmiş");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_SystemFolder_ShouldSetIsSystemTrue()
    {
        var folder = DocumentFolder.Create(_tenantId, "Siparişler", isSystem: true);
        folder.IsSystem.Should().BeTrue();
    }
}
