using FluentAssertions;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.ERP;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Infrastructure.ERP;

/// <summary>
/// ERPAdapterFactory integration tests — validates enum-based IErpAdapterFactory resolution.
/// Uses mock IErpAdapter instances for Logo, Netsis, and Nebim.
/// </summary>
[Trait("Category", "Integration")]
public class ErpProviderFactoryTests
{
    private readonly ERPAdapterFactory _sut;

    public ErpProviderFactoryTests()
    {
        var logoAdapter = new Mock<IErpAdapter>();
        logoAdapter.Setup(a => a.Provider).Returns(ErpProvider.Logo);

        var netsisAdapter = new Mock<IErpAdapter>();
        netsisAdapter.Setup(a => a.Provider).Returns(ErpProvider.Netsis);

        var nebimAdapter = new Mock<IErpAdapter>();
        nebimAdapter.Setup(a => a.Provider).Returns(ErpProvider.Nebim);

        var erpAdapters = new List<IErpAdapter>
        {
            logoAdapter.Object,
            netsisAdapter.Object,
            nebimAdapter.Object
        };

        // Legacy adapters — empty for these tests
        var legacyAdapters = new List<IERPAdapter>();

        _sut = new ERPAdapterFactory(legacyAdapters, erpAdapters);
    }

    [Fact]
    public void Create_Logo_ReturnsLogoAdapter()
    {
        // Act
        var adapter = _sut.GetAdapter(ErpProvider.Logo);

        // Assert
        adapter.Should().NotBeNull();
        adapter.Provider.Should().Be(ErpProvider.Logo);
    }

    [Fact]
    public void Create_Netsis_ReturnsNetsisAdapter()
    {
        // Act
        var adapter = _sut.GetAdapter(ErpProvider.Netsis);

        // Assert
        adapter.Should().NotBeNull();
        adapter.Provider.Should().Be(ErpProvider.Netsis);
    }

    [Fact]
    public void Create_Nebim_ReturnsNebimAdapter()
    {
        // Act
        var adapter = _sut.GetAdapter(ErpProvider.Nebim);

        // Assert
        adapter.Should().NotBeNull();
        adapter.Provider.Should().Be(ErpProvider.Nebim);
    }

    [Fact]
    public void Create_None_ThrowsNotSupported()
    {
        // Act
        var act = () => _sut.GetAdapter(ErpProvider.None);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*None*");
    }

    [Fact]
    public void GetAvailableProviders_ReturnsNonEmpty()
    {
        // Act
        var providers = _sut.SupportedProviders;

        // Assert
        providers.Should().NotBeEmpty();
        providers.Should().HaveCountGreaterOrEqualTo(3);
        providers.Should().Contain(ErpProvider.Logo);
        providers.Should().Contain(ErpProvider.Netsis);
        providers.Should().Contain(ErpProvider.Nebim);
    }
}
