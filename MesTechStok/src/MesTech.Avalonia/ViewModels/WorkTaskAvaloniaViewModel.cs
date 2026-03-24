using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Gorevler ViewModel — gorev listesi + durum takibi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class WorkTaskAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<WorkTaskItemDto> Tasks { get; } = [];

    public WorkTaskAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(200); // Will be replaced with MediatR query

            Tasks.Clear();
            Tasks.Add(new WorkTaskItemDto { TaskName = "Trendyol fiyat senkronizasyonu", AssignedTo = "Ali Veli", Priority = "Yuksek", Status = "Devam Ediyor", DueDate = "20.03.2026" });
            Tasks.Add(new WorkTaskItemDto { TaskName = "Hepsiburada urun guncelleme", AssignedTo = "Mehmet Demir", Priority = "Orta", Status = "Bekliyor", DueDate = "21.03.2026" });
            Tasks.Add(new WorkTaskItemDto { TaskName = "Stok sayimi — Ana Depo", AssignedTo = "Fatma Ozturk", Priority = "Yuksek", Status = "Tamamlandi", DueDate = "18.03.2026" });
            Tasks.Add(new WorkTaskItemDto { TaskName = "Yeni tedarikci onboarding", AssignedTo = "Ali Veli", Priority = "Dusuk", Status = "Bekliyor", DueDate = "25.03.2026" });

            TotalCount = Tasks.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Gorevler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class WorkTaskItemDto
{
    public string TaskName { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
}
