using CommunityToolkit.Mvvm.ComponentModel;
using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Tests for MainWindowViewModel navigation logic.
/// These are pure ViewModel tests (no headless rendering needed)
/// that verify the navigation switch statement maps correctly.
/// </summary>
[Trait("Category", "Avalonia")]
[Trait("Layer", "ViewModel")]
public class ViewModelNavigationTests
{
    private readonly Mock<MesTech.Avalonia.Services.IViewModelFactory> _mockFactory;
    private readonly MainWindowViewModel _sut;

    public ViewModelNavigationTests()
    {
        _mockFactory = new Mock<MesTech.Avalonia.Services.IViewModelFactory>();
        _sut = new MainWindowViewModel(_mockFactory.Object);
    }

    [Fact]
    public void NavigateTo_Dashboard_SetsTitleToKontrolPaneli()
    {
        // Arrange — use a simple ObservableObject stub instead of mocking
        // DashboardAvaloniaViewModel (which creates KpiCardViewModel/AvaloniaObject
        // instances that require the Avalonia dispatcher thread).
        var stubVm = new StubViewModel();
        _mockFactory
            .Setup(f => f.Create("Dashboard"))
            .Returns(stubVm);

        // Act
        _sut.NavigateToCommand.Execute("Dashboard");

        // Assert
        _sut.CurrentViewTitle.Should().Be("Kontrol Paneli");
        _sut.CurrentView.Should().BeSameAs(stubVm);
    }

    [Theory]
    [InlineData("Products", "Urunler")]
    [InlineData("Stock", "Stok Yonetimi")]
    [InlineData("Orders", "Siparis Yonetimi")]
    [InlineData("Settings", "Ayarlar")]
    [InlineData("Leads", "Potansiyel Musteriler")]
    [InlineData("Kanban", "Firsatlar — Kanban")]
    [InlineData("Contacts", "CRM Kisiler")]
    [InlineData("Employees", "Calisanlar")]
    [InlineData("LeaveRequests", "Izin Talepleri")]
    [InlineData("Documents", "Belge Yonetimi")]
    [InlineData("Reports", "Raporlar")]
    [InlineData("Marketplaces", "Pazaryerleri")]
    [InlineData("Expenses", "Giderler")]
    [InlineData("BankAccounts", "Banka Hesaplari")]
    public void NavigateTo_ValidViewName_SetsCorrectTitle(string viewName, string expectedTitle)
    {
        // Act
        _sut.NavigateToCommand.Execute(viewName);

        // Assert
        _sut.CurrentViewTitle.Should().Be(expectedTitle);
    }

    [Fact]
    public void NavigateTo_UnknownViewName_KeepsCurrentView()
    {
        // Arrange — set initial state
        _sut.NavigateToCommand.Execute("Dashboard");
        var initialTitle = _sut.CurrentViewTitle;

        // Act
        _sut.NavigateToCommand.Execute("NonExistentView");

        // Assert — title and view should remain unchanged
        _sut.CurrentViewTitle.Should().Be(initialTitle);
    }

    [Fact]
    public void ViewModel_InitialState_HasDefaultTitle()
    {
        // Assert
        _sut.CurrentViewTitle.Should().Be("Dashboard");
        _sut.CurrentView.Should().BeNull();
    }
}

/// <summary>
/// Lightweight ObservableObject stub for navigation tests.
/// Avoids creating DashboardAvaloniaViewModel (which depends on Avalonia dispatcher).
/// </summary>
internal sealed class StubViewModel : ObservableObject { }
