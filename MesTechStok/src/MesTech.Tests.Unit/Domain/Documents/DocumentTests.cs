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
        ev.FileName.Should().Be("Fatura-Ocak.pdf");
        ev.FileSizeBytes.Should().Be(102400);
    }

    [Fact]
    public void Create_EmptyFileName_ShouldThrow()
    {
        var act = () => Document.Create(_tenantId, "", "Orijinal.pdf",
            "application/pdf", 1000, "/path/to/file", _userId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ZeroSize_ShouldThrow()
    {
        var act = () => Document.Create(_tenantId, "file.pdf", "file.pdf",
            "application/pdf", 0, "/path", _userId);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_EmptyStoragePath_ShouldThrow()
    {
        var act = () => Document.Create(_tenantId, "file.pdf", "file.pdf",
            "application/pdf", 1000, "", _userId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LinkToOrder_ShouldSetOrderId()
    {
        var doc = CreateDocument();
        var orderId = Guid.NewGuid();
        doc.LinkToOrder(orderId);
        doc.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void MoveToFolder_ShouldUpdateFolderId()
    {
        var doc = CreateDocument();
        var folderId = Guid.NewGuid();
        doc.MoveToFolder(folderId);
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
    public void Delete_SystemFolder_ShouldThrow()
    {
        var folder = DocumentFolder.Create(_tenantId, "Siparişler", isSystem: true);
        var act = () => folder.Delete();
        act.Should().Throw<InvalidOperationException>();
    }
}
