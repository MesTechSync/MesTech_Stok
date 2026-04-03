using FluentAssertions;
using MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class DeleteErpAccountMappingHandlerTests
{
    private readonly Mock<IErpAccountMappingRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<DeleteErpAccountMappingHandler>> _loggerMock = new();

    private DeleteErpAccountMappingHandler CreateHandler() =>
        new(_repoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_MappingNotFound_ShouldReturnFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErpAccountMapping?)null);

        var command = new DeleteErpAccountMappingCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().BeFalse();
        _repoMock.Verify(r => r.Remove(It.IsAny<ErpAccountMapping>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TenantMismatch_ShouldReturnFalse()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var mapping = ErpAccountMapping.Create(
            tenantA, Domain.Enums.ErpProvider.Parasut,
            "100", "Name", "Type", "600", "ErpName");

        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        var command = new DeleteErpAccountMappingCommand(tenantB, mapping.Id);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().BeFalse();
        _repoMock.Verify(r => r.Remove(It.IsAny<ErpAccountMapping>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnTrue()
    {
        var tenantId = Guid.NewGuid();
        var mapping = ErpAccountMapping.Create(
            tenantId, Domain.Enums.ErpProvider.Parasut,
            "100", "Name", "Type", "600", "ErpName");

        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        var command = new DeleteErpAccountMappingCommand(tenantId, mapping.Id);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.Remove(mapping), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
