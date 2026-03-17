using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// CreateSalaryRecordHandler tests — valid salary creation and zero gross validation.
/// </summary>
[Trait("Category", "Unit")]
public class CreateSalaryRecordHandlerTests
{
    private readonly Mock<ISalaryRecordRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly CreateSalaryRecordHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateSalaryRecordHandlerTests()
    {
        _repoMock = new Mock<ISalaryRecordRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new CreateSalaryRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuidAndPersists()
    {
        // Arrange
        var command = new CreateSalaryRecordCommand(
            _tenantId,
            "Ahmet Yilmaz",
            25000m,    // gross
            5625m,     // SGK employer
            3500m,     // SGK employee
            3750m,     // income tax
            189m,      // stamp tax
            2026,
            3,
            Guid.NewGuid(),
            "Mart 2026 maasi");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<SalaryRecord>(s =>
                s.EmployeeName == "Ahmet Yilmaz" &&
                s.GrossSalary == 25000m &&
                s.Year == 2026 &&
                s.Month == 3),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_ZeroGrossSalary_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreateSalaryRecordCommand(
            _tenantId,
            "Mehmet Demir",
            0m,    // zero gross — not allowed
            0m, 0m, 0m, 0m,
            2026, 3);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Gross salary must be positive*");

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<SalaryRecord>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_NegativeGrossSalary_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreateSalaryRecordCommand(
            _tenantId,
            "Negative Test",
            -5000m,
            0m, 0m, 0m, 0m,
            2026, 3);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Handle_InvalidMonth_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreateSalaryRecordCommand(
            _tenantId,
            "Ay Hatasi",
            10000m,
            2250m, 1400m, 1500m, 75m,
            2026,
            13); // invalid month

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Month must be between 1 and 12*");
    }

    [Fact]
    public async Task Handle_InvalidYear_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreateSalaryRecordCommand(
            _tenantId,
            "Yil Hatasi",
            10000m,
            2250m, 1400m, 1500m, 75m,
            1999, // invalid year
            6);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Year must be between 2000 and 2100*");
    }
}
