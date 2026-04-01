#pragma warning disable CS1998
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Feed Olusturma ViewModel — URL, format, sync araligi, fiyat markup, min marj, kolon eslestirme.
/// </summary>
public partial class FeedCreateAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private string feedName = string.Empty;
    [ObservableProperty] private string feedUrl = string.Empty;
    [ObservableProperty] private string selectedFormat = "XML";
    [ObservableProperty] private int syncIntervalMinutes = 60;
    [ObservableProperty] private decimal priceMarkupPercent = 15.0m;
    [ObservableProperty] private decimal minimumMarginPercent = 5.0m;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool saveCompleted;

    public ObservableCollection<string> FormatOptions { get; } = ["XML", "CSV", "API", "Excel"];
    public ObservableCollection<FeedColumnMappingDto> ColumnMappings { get; } = [];

    public FeedCreateAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
        InitDefaultMappings();
    }

    private void InitDefaultMappings()
    {
        ColumnMappings.Add(new FeedColumnMappingDto { SourceColumn = "urun_adi", TargetField = "Name" });
        ColumnMappings.Add(new FeedColumnMappingDto { SourceColumn = "fiyat", TargetField = "Price" });
        ColumnMappings.Add(new FeedColumnMappingDto { SourceColumn = "stok", TargetField = "Stock" });
        ColumnMappings.Add(new FeedColumnMappingDto { SourceColumn = "barkod", TargetField = "Barcode" });
        ColumnMappings.Add(new FeedColumnMappingDto { SourceColumn = "kategori", TargetField = "Category" });
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // Reset form to clean initial state
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
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

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
            var format = Enum.TryParse<FeedFormat>(SelectedFormat, out var f) ? f : FeedFormat.Xml;
            await _mediator.Send(new CreateFeedSourceCommand(
                _tenantProvider.GetCurrentTenantId(), FeedName, FeedUrl, format,
                PriceMarkupPercent, 0, SyncIntervalMinutes, null, true));
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
        ColumnMappings.Add(new FeedColumnMappingDto { SourceColumn = "", TargetField = "" });
    }

    [RelayCommand]
    private void RemoveMapping(FeedColumnMappingDto? mapping)
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

public class FeedColumnMappingDto
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
}
