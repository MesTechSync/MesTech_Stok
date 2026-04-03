using FluentAssertions;
using MediatR;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class AuditLogAvaloniaViewModelTests
{
    private static AuditLogAvaloniaViewModel CreateSut()
    {
        var senderMock = new Mock<ISender>();
        var logs = Enumerable.Range(1, 10).Select(i => new AccessLog
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Action = i % 2 == 0 ? "Update" : "Create",
            Resource = "Product",
            IsAllowed = true,
            AccessTime = DateTime.Now.AddMinutes(-i * 10),
            IpAddress = "192.168.1.1",
            UserAgent = "MesTech/1.0",
            AdditionalInfo = $"Test entry {i}"
        }).ToList();
        senderMock
            .Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AccessLog>)logs);
        return new AuditLogAvaloniaViewModel(senderMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsExported.Should().BeFalse();
        sut.ExportMessage.Should().BeEmpty();
        sut.IsDetailVisible.Should().BeFalse();
        sut.SelectedEntry.Should().BeNull();
        sut.SelectedUser.Should().Be("Tumu");
        sut.SelectedAction.Should().Be("Tumu");
        sut.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulate10Entries()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.LogEntries.Should().HaveCount(10);
        sut.IsEmpty.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public void ShowDetailCommand_ShouldSetSelectedEntryAndVisible()
    {
        // Arrange
        var sut = CreateSut();
        var entry = new AuditLogEntry("20.03.2026 14:32", "admin@mestech.com", "Update",
            "Product", "PRD-4521", "Fiyat guncellendi", "149.90", "159.90");

        // Act
        sut.ShowDetailCommand.Execute(entry);

        // Assert
        sut.SelectedEntry.Should().BeSameAs(entry);
        sut.IsDetailVisible.Should().BeTrue();
    }

    [Fact]
    public void CloseDetailCommand_ShouldClearSelectedEntry()
    {
        // Arrange
        var sut = CreateSut();
        var entry = new AuditLogEntry("20.03.2026 14:32", "admin@mestech.com", "Update",
            "Product", "PRD-4521", "Test", "old", "new");
        sut.ShowDetailCommand.Execute(entry);

        // Act
        sut.CloseDetailCommand.Execute(null);

        // Assert
        sut.SelectedEntry.Should().BeNull();
        sut.IsDetailVisible.Should().BeFalse();
    }

    [Fact]
    public async Task ExportCsvCommand_ShouldSetExportedState()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        await sut.ExportCsvCommand.ExecuteAsync(null);

        // Assert
        sut.IsExported.Should().BeTrue();
        sut.ExportMessage.Should().Contain("10 kayit");
        sut.IsLoading.Should().BeFalse();
    }
}
