using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 11: Invoice, Cargo, Doc, HR, Stock misc VM tests (G050)
// ════════════════════════════════════════════════════════

#region CargoProvidersAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CargoProvidersAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new CargoProvidersAvaloniaViewModel(Mock.Of<IMediator>());
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region DocumentsAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class DocumentsAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new DocumentsAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>());
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region BankAccountsAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class BankAccountsAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new BankAccountsAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>());
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region DepartmentAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class DepartmentAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new DepartmentAvaloniaViewModel(Mock.Of<IMediator>());
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region EmployeesAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class EmployeesAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new EmployeesAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>());
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region WarehouseSummaryAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class WarehouseSummaryAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new WarehouseSummaryAvaloniaViewModel(Mock.Of<IMediator>());
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region ProductFetchAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ProductFetchAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new ProductFetchAvaloniaViewModel(Mock.Of<IMediator>());
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion
