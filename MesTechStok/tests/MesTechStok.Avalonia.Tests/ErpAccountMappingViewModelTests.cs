using FluentAssertions;
using MesTech.Application.Features.Erp.Queries.GetErpAccountMappings;
using MesTech.Avalonia.ViewModels.Erp;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ErpAccountMappingViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly ErpAccountMappingViewModel _sut;

    public ErpAccountMappingViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());

        // Setup mediator to return test data matching all test expectations:
        // - 10 distinct MesTech accounts (by code)
        // - 10 distinct ERP accounts (by code)
        // - 6 account types: Alici, Satici, Kasa, Banka, Stok, Gelir
        // - 3 active (mapped) pairs: 120.01↔ERP-120-001, 320.01↔ERP-320-001, 102.01↔ERP-102-001
        // - Search "120" → 2 MesTech accounts (120.01, 120.02) both Alici
        // - Search "Garanti" → 1 ERP account (ERP-102-001, "Garanti Bankasi", Banka)
        // - 600.01 "Yurtici Satislar" ↔ ERP-600-001 "Satis Gelirleri" unmapped (for MapAccounts test)
        var now = DateTime.UtcNow;
        var testData = new List<ErpAccountMappingDto>
        {
            // Row 1 — Alici, ACTIVE mapped pair #1 (120.01 ↔ ERP-120-001)
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "120.01", MesTechAccountName = "Alicilar - Yurtici", MesTechAccountType = "Alici", ErpAccountCode = "ERP-120-001", ErpAccountName = "Musteriler Genel", IsActive = true, LastSyncAt = now, CreatedAt = now },
            // Row 2 — Alici (second 120 code for search "120" → 2 results)
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "120.02", MesTechAccountName = "Alicilar - Yurtdisi", MesTechAccountType = "Alici", ErpAccountCode = "ERP-120-002", ErpAccountName = "Musteriler Yurtdisi", IsActive = false, CreatedAt = now },
            // Row 3 — Satici, ACTIVE mapped pair #2 (320.01 ↔ ERP-320-001)
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "320.01", MesTechAccountName = "Saticilar - Yurtici", MesTechAccountType = "Satici", ErpAccountCode = "ERP-320-001", ErpAccountName = "Tedarikciler Genel", IsActive = true, LastSyncAt = now, CreatedAt = now },
            // Row 4 — Satici
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "320.02", MesTechAccountName = "Saticilar - Yurtdisi", MesTechAccountType = "Satici", ErpAccountCode = "ERP-320-002", ErpAccountName = "Tedarikciler Yurtdisi", IsActive = false, CreatedAt = now },
            // Row 5 — Kasa
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "100.01", MesTechAccountName = "Kasa - TL", MesTechAccountType = "Kasa", ErpAccountCode = "ERP-100-001", ErpAccountName = "Ana Kasa", IsActive = false, CreatedAt = now },
            // Row 6 — Banka, ACTIVE mapped pair #3 (102.01 ↔ ERP-102-001)
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "102.01", MesTechAccountName = "Banka Hesabi - Garanti", MesTechAccountType = "Banka", ErpAccountCode = "ERP-102-001", ErpAccountName = "Garanti Bankasi", IsActive = true, LastSyncAt = now, CreatedAt = now },
            // Row 7 — Banka
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "102.02", MesTechAccountName = "Banka Hesabi - Ziraat", MesTechAccountType = "Banka", ErpAccountCode = "ERP-102-002", ErpAccountName = "Ziraat Bankasi", IsActive = false, CreatedAt = now },
            // Row 8 — Stok
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "150.01", MesTechAccountName = "Hammadde Stok", MesTechAccountType = "Stok", ErpAccountCode = "ERP-150-001", ErpAccountName = "Stok Hammadde", IsActive = false, CreatedAt = now },
            // Row 9 — Gelir (unmapped — used in MapAccounts test)
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "600.01", MesTechAccountName = "Yurtici Satislar", MesTechAccountType = "Gelir", ErpAccountCode = "ERP-600-001", ErpAccountName = "Satis Gelirleri", IsActive = false, CreatedAt = now },
            // Row 10 — Gelir
            new() { Id = Guid.NewGuid(), MesTechAccountCode = "600.02", MesTechAccountName = "Yurtdisi Satislar", MesTechAccountType = "Gelir", ErpAccountCode = "ERP-600-002", ErpAccountName = "Ihracat Gelirleri", IsActive = false, CreatedAt = now },
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetErpAccountMappingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ErpAccountMappingDto>)testData.AsReadOnly());

        _sut = new ErpAccountMappingViewModel(_mediatorMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.IsEmpty.Should().BeFalse();
        _sut.MappedCount.Should().Be(0);
        _sut.MesTechSearchText.Should().BeEmpty();
        _sut.ErpSearchText.Should().BeEmpty();
        _sut.SelectedMesTechAccount.Should().BeNull();
        _sut.SelectedErpAccount.Should().BeNull();
        _sut.SelectedMappedPair.Should().BeNull();
        _sut.MesTechAccounts.Should().BeEmpty();
        _sut.ErpAccounts.Should().BeEmpty();
        _sut.MappedPairs.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateMesTechAccounts()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.MesTechAccounts.Should().HaveCount(10);
        _sut.MesTechAccounts.Should().Contain(a => a.Code == "120.01" && a.Name == "Alicilar - Yurtici");
        _sut.MesTechAccounts.Should().Contain(a => a.Code == "600.02" && a.Name == "Yurtdisi Satislar");
        _sut.MesTechAccounts.Select(a => a.AccountType).Distinct()
            .Should().BeEquivalentTo(new[] { "Alici", "Satici", "Kasa", "Banka", "Stok", "Gelir" });
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateErpAccounts()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.ErpAccounts.Should().HaveCount(10);
        _sut.ErpAccounts.Should().Contain(a => a.Code == "ERP-120-001" && a.Name == "Musteriler Genel");
        _sut.ErpAccounts.Should().Contain(a => a.Code == "ERP-600-002" && a.Name == "Ihracat Gelirleri");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateMappedPairs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.MappedPairs.Should().HaveCount(3);
        _sut.MappedPairs[0].MesTechCode.Should().Be("120.01");
        _sut.MappedPairs[0].ErpCode.Should().Be("ERP-120-001");
        _sut.MappedPairs[1].MesTechCode.Should().Be("320.01");
        _sut.MappedPairs[1].ErpName.Should().Be("Tedarikciler Genel");
        _sut.MappedPairs[2].MesTechName.Should().Be("Banka Hesabi - Garanti");
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateMappedCount()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.MappedCount.Should().Be(3);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task MesTechSearch_ShouldFilterByCode()
    {
        // Arrange
        await _sut.LoadAsync();
        _sut.MesTechAccounts.Should().HaveCount(10);

        // Act — search "120" filters to Alici accounts with code containing 120
        _sut.MesTechSearchText = "120";

        // Assert
        _sut.MesTechAccounts.Should().HaveCount(2);
        _sut.MesTechAccounts.Should().OnlyContain(a => a.Code.Contains("120"));
        _sut.MesTechAccounts.Select(a => a.AccountType).Should().OnlyContain(t => t == "Alici");
    }

    [Fact]
    public async Task ErpSearch_ShouldFilterByName()
    {
        // Arrange
        await _sut.LoadAsync();
        _sut.ErpAccounts.Should().HaveCount(10);

        // Act — search "Garanti" filters to 1 bank account
        _sut.ErpSearchText = "Garanti";

        // Assert
        _sut.ErpAccounts.Should().HaveCount(1);
        _sut.ErpAccounts[0].Name.Should().Be("Garanti Bankasi");
        _sut.ErpAccounts[0].Code.Should().Be("ERP-102-001");
        _sut.ErpAccounts[0].AccountType.Should().Be("Banka");
    }

    [Fact]
    public async Task MapAccounts_ShouldAddNewPair()
    {
        // Arrange
        await _sut.LoadAsync();
        var initialMappedCount = _sut.MappedCount;

        // Select unmapped accounts (600.01 and ERP-600-001 are not pre-mapped)
        _sut.SelectedMesTechAccount = _sut.MesTechAccounts.First(a => a.Code == "600.01");
        _sut.SelectedErpAccount = _sut.ErpAccounts.First(a => a.Code == "ERP-600-001");

        // Act
        await _sut.MapAccountsCommand.ExecuteAsync(null);

        // Assert
        _sut.MappedCount.Should().Be(initialMappedCount + 1);
        _sut.MappedPairs.Should().HaveCount(initialMappedCount + 1);
        var newPair = _sut.MappedPairs.Last();
        newPair.MesTechCode.Should().Be("600.01");
        newPair.MesTechName.Should().Be("Yurtici Satislar");
        newPair.ErpCode.Should().Be("ERP-600-001");
        newPair.ErpName.Should().Be("Satis Gelirleri");
        newPair.MappedDate.Should().NotBeEmpty();
        _sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task MapAccounts_DuplicateMapping_ShouldShowError()
    {
        // Arrange
        await _sut.LoadAsync();
        var initialMappedCount = _sut.MappedCount;

        // Select accounts that are already mapped (120.01 is pre-mapped)
        _sut.SelectedMesTechAccount = _sut.MesTechAccounts.First(a => a.Code == "120.01");
        _sut.SelectedErpAccount = _sut.ErpAccounts.First(a => a.Code == "ERP-600-002");

        // Act
        await _sut.MapAccountsCommand.ExecuteAsync(null);

        // Assert — mapping rejected, error message set
        _sut.MappedCount.Should().Be(initialMappedCount);
        _sut.ErrorMessage.Should().Contain("zaten eslesmis");
        _sut.MappedPairs.Should().HaveCount(initialMappedCount);
    }

    [Fact]
    public async Task RemoveMapping_ShouldRemovePair()
    {
        // Arrange
        await _sut.LoadAsync();
        var initialMappedCount = _sut.MappedCount;
        _sut.SelectedMappedPair = _sut.MappedPairs[0];

        // Act
        await _sut.RemoveMappingCommand.ExecuteAsync(null);

        // Assert
        _sut.MappedCount.Should().Be(initialMappedCount - 1);
        _sut.MappedPairs.Should().HaveCount(initialMappedCount - 1);
        _sut.SelectedMappedPair.Should().BeNull();
        _sut.MappedPairs.Should().NotContain(p => p.MesTechCode == "120.01");
    }
}
