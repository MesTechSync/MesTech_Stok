using Moq;
using MesTech.Domain.Interfaces;

namespace MesTech.Tests.Unit._Shared;

/// <summary>
/// Ortak mock ve fixture tanimlamalari.
/// </summary>
public static class TestFixtures
{
    public static Mock<IProductRepository> MockProductRepository() => new();
    public static Mock<IStockMovementRepository> MockStockMovementRepository() => new();
    public static Mock<IUnitOfWork> MockUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return mock;
    }
}
