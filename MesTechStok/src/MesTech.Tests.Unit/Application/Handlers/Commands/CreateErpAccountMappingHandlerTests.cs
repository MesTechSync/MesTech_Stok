using FluentAssertions;
using MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CreateErpAccountMappingHandlerTests
{
    private readonly Mock<IErpAccountMappingRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CreateErpAccountMappingHandler>> _loggerMock = new();

    private CreateErpAccountMappingHandler CreateHandler() =>
        new(_repoMock.Object, _uowMock.Object, _loggerMock.Object);

    private static CreateErpAccountMappingCommand CreateCommand(
        string mesTechCode = "100-SATIS", string erpCode = "600-REVENUE") =>
        new(Guid.NewGuid(), mesTechCode, "Satis Geliri", "Gelir", erpCode, "Revenue Account");

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnSuccess()
    {
        _repoMock.Setup(r => r.FindByMesTechCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErpAccountMapping?)null);
        _repoMock.Setup(r => r.FindByErpCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErpAccountMapping?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.MappingId.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<ErpAccountMapping>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateMesTechCode_ShouldReturnFailure()
    {
        var existing = ErpAccountMapping.Create(
            Guid.NewGuid(), Domain.Enums.ErpProvider.Parasut,
            "100-SATIS", "Satis", "Gelir", "600", "Rev");
        _repoMock.Setup(r => r.FindByMesTechCodeAsync(It.IsAny<Guid>(), "100-SATIS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("100-SATIS");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<ErpAccountMapping>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateErpCode_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.FindByMesTechCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErpAccountMapping?)null);

        var existing = ErpAccountMapping.Create(
            Guid.NewGuid(), Domain.Enums.ErpProvider.Parasut,
            "200", "X", "Y", "600-REVENUE", "Rev");
        _repoMock.Setup(r => r.FindByErpCodeAsync(It.IsAny<Guid>(), "600-REVENUE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("600-REVENUE");
    }
}
