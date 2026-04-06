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

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private int totalCount;

    private readonly List<LeaveRequestItemDto> _allItems = [];

    public ObservableCollection<LeaveRequestItemDto> LeaveRequests { get; } = [];

    public LeaveRequestsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var requests = await _mediator.Send(new GetLeaveRequestsQuery(_currentUser.TenantId), ct);

            _allItems.Clear();
            foreach (var r in requests)
            {
                _allItems.Add(new LeaveRequestItemDto
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

            var pending = requests.Count(r => r.Status == "Beklemede");
            Summary = $"{_allItems.Count} izin talebi, {pending} onay bekliyor.";
            ApplyFilter();
        }, "Izin talepleri yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        LeaveRequests.Clear();
        var filtered = _allItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(r =>
                r.EmployeeName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.LeaveType.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Status.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var item in filtered)
            LeaveRequests.Add(item);
        TotalCount = LeaveRequests.Count;
        IsEmpty = LeaveRequests.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Add()
    {
        var newRequest = new LeaveRequestItemDto
        {
            EmployeeName = _currentUser.Username ?? "Bilinmiyor",
            LeaveType = "Yillik Izin",
            StartDate = DateTime.Now.ToString("dd.MM.yyyy"),
            EndDate = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy"),
            TotalDays = 1,
            Status = "Beklemede",
            Reason = string.Empty
        };
        _allItems.Insert(0, newRequest);
        ApplyFilter();
        await Task.CompletedTask;
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
