using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.SaveImportTemplate;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class SaveImportTemplateHandlerTests
{
    private readonly Mock<IImportTemplateRepository> _templateRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<SaveImportTemplateHandler>> _loggerMock = new();
    private readonly SaveImportTemplateHandler _sut;

    public SaveImportTemplateHandlerTests()
    {
        _sut = new SaveImportTemplateHandler(
            _templateRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithTemplateId()
    {
        // Arrange
        var command = new SaveImportTemplateCommand(
            TenantId: Guid.NewGuid(),
            TemplateName: "CSV Import v1",
            FileFormat: "CSV",
            ColumnMappings: new Dictionary<string, string>
            {
                { "Col_A", "Name" },
                { "Col_B", "SKU" }
            });

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TemplateId.Should().NotBe(Guid.Empty);
        result.ErrorMessage.Should().BeNull();

        _templateRepoMock.Verify(
            r => r.AddAsync(It.Is<ImportTemplate>(t =>
                t.Name == "CSV Import v1" &&
                t.Format == "CSV" &&
                t.FieldCount == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyColumnMappings_ReturnsSuccessWithZeroFields()
    {
        // Arrange
        var command = new SaveImportTemplateCommand(
            TenantId: Guid.NewGuid(),
            TemplateName: "Empty Template",
            FileFormat: "XML",
            ColumnMappings: new Dictionary<string, string>());

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TemplateId.Should().NotBe(Guid.Empty);

        _templateRepoMock.Verify(
            r => r.AddAsync(It.Is<ImportTemplate>(t => t.FieldCount == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFailureWithMessage()
    {
        // Arrange
        var command = new SaveImportTemplateCommand(
            TenantId: Guid.NewGuid(),
            TemplateName: "Failing Template",
            FileFormat: "CSV",
            ColumnMappings: new Dictionary<string, string> { { "A", "B" } });

        _templateRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ImportTemplate>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection lost"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DB connection lost");
        result.TemplateId.Should().Be(Guid.Empty);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyTenantId_ReturnsFailure()
    {
        // Arrange — ImportTemplate.Create throws ArgumentException for Guid.Empty
        var command = new SaveImportTemplateCommand(
            TenantId: Guid.Empty,
            TemplateName: "Bad Tenant",
            FileFormat: "CSV",
            ColumnMappings: new Dictionary<string, string> { { "X", "Y" } });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();

        _templateRepoMock.Verify(
            r => r.AddAsync(It.IsAny<ImportTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
