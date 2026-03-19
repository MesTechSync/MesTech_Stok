using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class PipelineAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string totalValue = "0 TL";

    public ObservableCollection<PipelineStageVm> Stages { get; } = [];

    public PipelineAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(50);
            Stages.Clear();

            Stages.Add(new PipelineStageVm { Name = "Ilk Iletisim", DealCount = 8, Amount = "120.000 TL", Percentage = 30, Color = "#3B82F6" });
            Stages.Add(new PipelineStageVm { Name = "Teklif Verildi", DealCount = 5, Amount = "89.000 TL", Percentage = 22, Color = "#F59E0B" });
            Stages.Add(new PipelineStageVm { Name = "Muzakere", DealCount = 3, Amount = "67.000 TL", Percentage = 16, Color = "#8B5CF6" });
            Stages.Add(new PipelineStageVm { Name = "Kazanildi", DealCount = 4, Amount = "145.000 TL", Percentage = 32, Color = "#10B981" });

            TotalCount = 20;
            TotalValue = "421.000 TL";
            IsEmpty = Stages.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Pipeline yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class PipelineStageVm
{
    public string Name { get; set; } = string.Empty;
    public int DealCount { get; set; }
    public string Amount { get; set; } = "0 TL";
    public int Percentage { get; set; }
    public string Color { get; set; } = "#3B82F6";
}
