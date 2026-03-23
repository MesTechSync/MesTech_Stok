using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class GLTransactionAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GLTransactionAvaloniaViewModel _sut;

    public GLTransactionAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new GLTransactionAvaloniaViewModel(_mediatorMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SelectedAccount.Should().Be("Tum Hesaplar");
        _sut.SearchText.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
        _sut.Transactions.Should().BeEmpty();
        _sut.Accounts.Should().HaveCount(8);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateTransactions()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — 6 demo transactions
        _sut.Transactions.Should().HaveCount(6);
        _sut.TotalCount.Should().Be(6);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.Transactions.Should().Contain(t => t.Account.Contains("Kasa"));
        _sut.Transactions.Should().Contain(t => t.Account.Contains("Satislar"));
    }

    [Fact]
    public async Task FilterByAccount_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedAccount = "100 - Kasa";

        // Assert
        _sut.Transactions.Should().HaveCount(1);
        _sut.Transactions[0].Description.Should().Contain("Kasa acilis");
        _sut.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchText_ShouldFilterByDescriptionOrVoucherNo()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Kargo";

        // Assert
        _sut.Transactions.Should().HaveCount(2); // Kargo gideri + Kargo odemesi
        _sut.Transactions.Should().OnlyContain(t =>
            t.Description.Contains("Kargo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FilterByAccount_NonExistentInData_ShouldShowEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedAccount = "320 - Saticilar";

        // Assert
        _sut.Transactions.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
        _sut.IsEmpty.Should().BeTrue();
    }
}
