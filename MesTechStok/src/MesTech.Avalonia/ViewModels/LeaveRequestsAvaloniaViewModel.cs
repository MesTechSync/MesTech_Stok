using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Hr.Queries.GetLeaveRequests;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Izin Talepleri ViewModel — HR modulu izin basvuru/onay listesi.
/// </summary>
public partial class LeaveRequestsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<LeaveRequestItemDto> LeaveRequests { get; } = [];

    public LeaveRequestsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var requests = await _mediator.Send(new GetLeaveRequestsQuery(_currentUser.TenantId));

            LeaveRequests.Clear();
            foreach (var r in requests)
            {
                LeaveRequests.Add(new LeaveRequestItemDto
                {
                    EmployeeName = r.EmployeeName,
                    LeaveType = r.LeaveType,
                    StartDate = r.StartDate.ToString("dd.MM.yyyy"),
                    EndDate = r.EndDate.ToString("dd.MM.yyyy"),
                    TotalDays = r.TotalDays,
                    Status = r.Status,
                    Reason = r.Reason ?? ""
                });
            }

            TotalCount = LeaveRequests.Count;
            var pending = requests.Count(r => r.Status == "Beklemede");
            Summary = $"{TotalCount} izin talebi, {pending} onay bekliyor.";
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Izin talepleri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Add()
    {
        await _dialog.ShowInfoAsync("Bu özellik yakinda aktif olacak.", "MesTech");
    }
}

public class LeaveRequestItemDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
