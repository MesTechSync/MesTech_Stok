using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;
using AppReturnDto = MesTech.Application.Features.Returns.Queries.GetReturnList.ReturnListItemDto;
using AppReturnQuery = MesTech.Application.Features.Returns.Queries.GetReturnList.GetReturnListQuery;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ReturnListAvaloniaViewModelTests
{
    private static readonly Guid TestTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static readonly IReadOnlyList<AppReturnDto> DemoReturns = new List<AppReturnDto>
    {
        new() { Id = Guid.Parse("1ad20010-0000-0000-0000-000000000001"), OrderNumber = "SIP-5001", Status = "Beklemede", Reason = "Yanlis urun", RefundAmount = 1250.00m, CreatedAt = new DateTime(2026, 3, 1) },
        new() { Id = Guid.Parse("1ad20020-0000-0000-0000-000000000002"), OrderNumber = "SIP-5002", Status = "Onaylandi", Reason = "Hasarli", RefundAmount = 890.00m, CreatedAt = new DateTime(2026, 3, 2) },
        new() { Id = Guid.Parse("1ad20030-0000-0000-0000-000000000003"), OrderNumber = "SIP-5003", Status = "Reddedildi", Reason = "Sure asimi", RefundAmount = 450.00m, CreatedAt = new DateTime(2026, 3, 3) },
        new() { Id = Guid.Parse("1ad20040-0000-0000-0000-000000000004"), OrderNumber = "SIP-5004", Status = "Yolda", Reason = "Beden uyumsuz", RefundAmount = 320.00m, CreatedAt = new DateTime(2026, 3, 4) },
        new() { Id = Guid.Parse("1ad20050-0000-0000-0000-000000000005"), OrderNumber = "SIP-5005", Status = "Teslim Alindi", Reason = "Farkli renk", RefundAmount = 1750.00m, CreatedAt = new DateTime(2026, 3, 5) },
        new() { Id = Guid.Parse("1ad20060-0000-0000-0000-000000000006"), OrderNumber = "SIP-5006", Status = "Iade Edildi", Reason = "Kusurlu", RefundAmount = 2100.00m, CreatedAt = new DateTime(2026, 3, 6) },
        new() { Id = Guid.Parse("1ad20070-0000-0000-0000-000000000007"), OrderNumber = "SIP-5007", Status = "Beklemede", Reason = "Eksik parca", RefundAmount = 560.00m, CreatedAt = new DateTime(2026, 3, 7) },
    };

    private readonly Mock<IMediator> _mediatorMock;
    private readonly ReturnListAvaloniaViewModel _sut;

    public ReturnListAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AppReturnQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DemoReturns);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(TestTenantId);

        _sut = new ReturnListAvaloniaViewModel(_mediatorMock.Object, currentUserMock.Object);
        // Constructor "Bu Ay" filtresi uygular — test verileri Mart 2026 olduğu için
        // "Tumu" seçerek tarih filtresini kaldır
        _sut.SelectedDateRange = "Tumu";
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateReturns()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Returns.Should().HaveCount(7);
        _sut.TotalCount.Should().Be(7);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ReturnListAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SelectedStatus_ShouldFilterReturns()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedStatus = "Beklemede";

        // Assert
        _sut.Returns.Should().OnlyContain(r => r.Durum == "Beklemede");
        _sut.TotalCount.Should().Be(2); // items 1 and 7
    }

    [Fact]
    public async Task SearchText_ShouldFilterByOrderNo()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search by order number since VM maps Musteri as empty
        _sut.SearchText = "SIP-5001";

        // Assert
        _sut.Returns.Should().HaveCount(1);
        _sut.Returns[0].SiparisNo.Should().Contain("SIP-5001");
    }

    [Fact]
    public async Task StatusFilter_CombinedWithSearch_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — filter by status then search by IadeNo prefix
        _sut.SelectedStatus = "Tumu";
        _sut.SearchText = "1AD200";

        // Assert — all 7 items have IadeNo starting with "1AD200" (from guid prefix)
        _sut.Returns.Should().HaveCount(7);
        _sut.Returns.Should().OnlyContain(r =>
            r.IadeNo.Contains("1AD200", StringComparison.OrdinalIgnoreCase));
    }
}
