using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for HR Leave Requests screen — Dalga 11.
/// Wired to IMediator — will use GetLeaveRequestsQuery when HR module is implemented.
/// </summary>
public partial class LeaveRequestsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string summary = "Izin talepleri ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int totalCount;

    public LeaveRequestsAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // TODO: Wire to GetLeaveRequestsQuery when HR module CQRS is implemented
            TotalCount = 0;
            Summary = "Izin talepleri ekrani hazir. Izin basvurusu, onay sureci ve yillik izin takibi burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void Add()
    {
        // TODO: Navigate to leave request create form
    }
}
