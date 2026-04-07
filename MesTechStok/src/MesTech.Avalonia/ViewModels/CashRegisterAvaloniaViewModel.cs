using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Finance.Queries.GetCashRegisters;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Kasa Yönetimi ViewModel — handler var, view yoktu.
/// GetCashRegistersQuery ile veri çeker.
/// </summary>
public partial class CashRegisterAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private decimal totalBalance;

    public ObservableCollection<CashRegisterDto> Registers { get; } = [];

    public CashRegisterAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetCashRegistersQuery(_currentUser.TenantId), ct);
            Registers.Clear();
            foreach (var r in result) Registers.Add(r);
            TotalCount = result.Count;
            TotalBalance = result.Sum(r => r.Balance);
            IsEmpty = result.Count == 0;
        }, "Kasa bilgileri yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
