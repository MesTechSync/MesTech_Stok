using FluentAssertions;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Application.Commands.RejectReturn;
using MesTech.Application.Features.Returns.Queries.GetReturnList;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;
using ReturnDto = MesTech.Application.Features.Returns.Queries.GetReturnList.ReturnListItemDto;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5: ReturnDetailAvaloniaViewModel tests (G407)
// Coverage: Constructor, LoadAsync, Approve, Reject, error handling
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ReturnDetailAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ICurrentUserService> _userMock;
    private readonly ReturnDetailAvaloniaViewModel _sut;

    public ReturnDetailAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _userMock = new Mock<ICurrentUserService>();
        _userMock.Setup(u => u.TenantId).Returns(Guid.NewGuid());
        _sut = new ReturnDetailAvaloniaViewModel(_mediatorMock.Object, _userMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.Timeline.Should().NotBeNull().And.BeEmpty();
        _sut.RejectReasons.Should().HaveCountGreaterThan(0);
        _sut.SelectedRejectReason.Should().Be("Diger");
    }

    [Fact]
    public async Task LoadAsync_WhenReturnsExist_PopulatesFields()
    {
        var returnItem = new ReturnDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            RefundAmount = 150.50m,
            Reason = "Urun hasarli",
            Status = "Beklemede",
            CreatedAt = new DateTime(2026, 3, 15)
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetReturnListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ReturnDto>)new List<ReturnDto> { returnItem });

        await _sut.LoadAsync();

        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SiparisNo.Should().Be("ORD-001");
        _sut.Tutar.Should().Be(150.50m);
        _sut.Sebep.Should().Be("Urun hasarli");
        _sut.Durum.Should().Be("Beklemede");
        _sut.Timeline.Should().HaveCount(4);
    }

    [Fact]
    public async Task LoadAsync_WhenEmpty_SetsIsEmpty()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetReturnListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ReturnDto>)new List<ReturnDto>());

        await _sut.LoadAsync();

        _sut.IsEmpty.Should().BeTrue();
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenThrows_SetsHasError()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetReturnListQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        await _sut.LoadAsync();

        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Contain("yuklenirken hata");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_AlwaysSetsIsLoadingFalse_InFinally()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetReturnListQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("any"));

        await _sut.LoadAsync();

        _sut.IsLoading.Should().BeFalse(); // KÇ-13: finally { IsLoading = false; }
    }

    [Fact]
    public async Task ApproveAsync_WhenSuccess_UpdatesDurum()
    {
        // Setup: load a return first
        var returnItem = new ReturnDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-002",
            RefundAmount = 99m,
            Reason = "Yanlis urun",
            Status = "Beklemede",
            CreatedAt = DateTime.Now
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetReturnListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ReturnDto>)new List<ReturnDto> { returnItem });
        await _sut.LoadAsync();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ApproveReturnCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproveReturnResult { IsSuccess = true });

        await _sut.ApproveCommand.ExecuteAsync(null);

        _sut.Durum.Should().Be("Onaylandi");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task RejectAsync_WhenSuccess_UpdatesDurum()
    {
        var returnItem = new ReturnDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-003",
            RefundAmount = 50m,
            Reason = "Fikirden vazgecme",
            Status = "Beklemede",
            CreatedAt = DateTime.Now
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetReturnListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ReturnDto>)new List<ReturnDto> { returnItem });
        await _sut.LoadAsync();

        _mediatorMock.Setup(m => m.Send(It.IsAny<RejectReturnCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RejectReturnResult { IsSuccess = true });

        _sut.SelectedRejectReason = "Urun kullanilmis";
        await _sut.RejectCommand.ExecuteAsync(null);

        _sut.Durum.Should().Be("Reddedildi");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveAsync_WhenFails_SetsHasError()
    {
        var returnItem = new ReturnDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-004",
            RefundAmount = 25m,
            Status = "Beklemede",
            CreatedAt = DateTime.Now
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetReturnListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ReturnDto>)new List<ReturnDto> { returnItem });
        await _sut.LoadAsync();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ApproveReturnCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproveReturnResult { IsSuccess = false, ErrorMessage = "Stok yetersiz" });

        await _sut.ApproveCommand.ExecuteAsync(null);

        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Contain("Onay islemi basarisiz");
        _sut.IsLoading.Should().BeFalse();
    }
}
