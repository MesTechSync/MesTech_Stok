using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Feed Olusturma ViewModel — URL, format, sync araligi, fiyat markup, min marj, kolon eslestirme.
/// TODO: Replace with MediatR.Send(new CreateSupplierFeedCommand()) when A1 CQRS is ready.
/// </summary>
public partial class FeedCreateAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string feedName = string.Empty;
    [ObservableProperty] private string feedUrl = string.Empty;
    [ObservableProperty] private string selectedFormat = "XML";
    [ObservableProperty] private int syncIntervalMinutes = 60;
    [ObservableProperty] private decimal priceMarkupPercent = 15.0m;
    [ObservableProperty] private decimal minimumMarginPercent = 5.0m;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool saveCompleted;

    public ObservableCollection<string> FormatOptions { get; } = ["XML", "CSV", "API", "Excel"];
    public ObservableCollection<ColumnMappingDto> ColumnMappings { get; } = [];

    public FeedCreateAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        InitDefaultMappings();
    }

    private void InitDefaultMappings()
    {
        ColumnMappings.Add(new ColumnMappingDto { SourceColumn = "urun_adi", TargetField = "Name" });
        ColumnMappings.Add(new ColumnMappingDto { SourceColumn = "fiyat", TargetField = "Price" });
        ColumnMappings.Add(new ColumnMappingDto { SourceColumn = "stok", TargetField = "Stock" });
        ColumnMappings.Add(new ColumnMappingDto { SourceColumn = "barkod", TargetField = "Barcode" });
        ColumnMappings.Add(new ColumnMappingDto { SourceColumn = "kategori", TargetField = "Category" });
    }

    [RelayCommand]
    private async Task SaveFeedAsync()
    {
        if (string.IsNullOrWhiteSpace(FeedName) || string.IsNullOrWhiteSpace(FeedUrl))
        {
            HasError = true;
            ErrorMessage = "Feed adi ve URL zorunludur.";
            return;
        }

        IsSaving = true;
        HasError = false;
        SaveCompleted = false;
        try
        {
            // TODO: Replace with MediatR.Send(new CreateSupplierFeedCommand()) when A1 CQRS is ready
            await Task.Delay(600);
            SaveCompleted = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Feed kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void AddMapping()
    {
        ColumnMappings.Add(new ColumnMappingDto { SourceColumn = "", TargetField = "" });
    }

    [RelayCommand]
    private void RemoveMapping(ColumnMappingDto? mapping)
    {
        if (mapping is not null)
            ColumnMappings.Remove(mapping);
    }

    [RelayCommand]
    private void Reset()
    {
        FeedName = string.Empty;
        FeedUrl = string.Empty;
        SelectedFormat = "XML";
        SyncIntervalMinutes = 60;
        PriceMarkupPercent = 15.0m;
        MinimumMarginPercent = 5.0m;
        SaveCompleted = false;
        ColumnMappings.Clear();
        InitDefaultMappings();
    }
}

public class ColumnMappingDto
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
}
