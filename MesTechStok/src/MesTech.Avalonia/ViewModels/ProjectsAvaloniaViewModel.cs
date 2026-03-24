using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class ProjectsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStatus;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ProjectItemVm> Projects { get; } = [];
    public string[] StatusOptions { get; } = ["Tumu", "Planlandi", "Devam Ediyor", "Tamamlandi", "Beklemede"];

    public ProjectsAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(50);
            Projects.Clear();
            Projects.Add(new ProjectItemVm { Id = Guid.NewGuid(), Name = "MesTech v2.0 Migration", Status = "Devam Ediyor", StartDate = DateTime.Now.AddMonths(-2), EndDate = DateTime.Now.AddMonths(1), Progress = 65, Manager = "Fatih I." });
            Projects.Add(new ProjectItemVm { Id = Guid.NewGuid(), Name = "Trendyol Entegrasyonu", Status = "Tamamlandi", StartDate = DateTime.Now.AddMonths(-4), EndDate = DateTime.Now.AddMonths(-1), Progress = 100, Manager = "Mehmet C." });
            Projects.Add(new ProjectItemVm { Id = Guid.NewGuid(), Name = "MESA AI Modulu", Status = "Devam Ediyor", StartDate = DateTime.Now.AddMonths(-1), EndDate = DateTime.Now.AddMonths(2), Progress = 35, Manager = "Ali K." });
            Projects.Add(new ProjectItemVm { Id = Guid.NewGuid(), Name = "Mobil Uygulama", Status = "Planlandi", StartDate = DateTime.Now.AddMonths(1), EndDate = DateTime.Now.AddMonths(4), Progress = 0, Manager = "Zeynep A." });
            Projects.Add(new ProjectItemVm { Id = Guid.NewGuid(), Name = "Muhasebe Modulu", Status = "Devam Ediyor", StartDate = DateTime.Now.AddMonths(-1), EndDate = DateTime.Now.AddMonths(1), Progress = 50, Manager = "Ayse K." });
            TotalCount = Projects.Count;
            IsEmpty = Projects.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Projeler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    partial void OnSelectedStatusChanged(string? value)
        => _ = LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }
}

public class ProjectItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Progress { get; set; }
    public string? Manager { get; set; }

    public string DateRange => $"{StartDate:dd.MM.yyyy} — {EndDate?.ToString("dd.MM.yyyy") ?? "—"}";
    public string ProgressDisplay => $"%{Progress}";
}
