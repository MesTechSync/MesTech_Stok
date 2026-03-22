using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateSalaryRecordHandlerTests
{
    private readonly Mock<ISalaryRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateSalaryRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateSalaryRecordHandlerTests()
    {
        _sut = new CreateSalaryRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateSalaryRecordCommand(
            TenantId, "Ali Veli", 25000m, 3750m, 3500m, 3750m, 189.75m, 2026, 3);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<SalaryRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmployeeId_ShouldCreateSuccessfully()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var command = new CreateSalaryRecordCommand(
            TenantId, "Mehmet Yilmaz", 20000m, 3000m, 2800m, 3000m, 152m, 2026, 3,
            EmployeeId: employeeId, Notes: "Mart maas");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
