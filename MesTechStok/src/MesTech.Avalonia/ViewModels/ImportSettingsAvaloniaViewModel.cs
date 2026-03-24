using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Icerik Aktarma Ayarlari ViewModel — kolon eslestirme sablonlari yonetimi.
/// </summary>
public partial class ImportSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    // Edit mode
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string editTemplateName = string.Empty;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool saveCompleted;

    public ObservableCollection<ImportTemplateDto> Templates { get; } = [];
    public ObservableCollection<ImportFieldMappingDto> EditMappings { get; } = [];

    public ImportSettingsAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(200);

            Templates.Clear();
            Templates.Add(new ImportTemplateDto { Name = "Trendyol XML Sablonu", FieldCount = 12, LastUsed = "19.03.2026", Format = "XML" });
            Templates.Add(new ImportTemplateDto { Name = "Genel CSV Sablonu", FieldCount = 8, LastUsed = "18.03.2026", Format = "CSV" });
            Templates.Add(new ImportTemplateDto { Name = "Hepsiburada Excel Sablonu", FieldCount = 15, LastUsed = "17.03.2026", Format = "Excel" });
            Templates.Add(new ImportTemplateDto { Name = "N11 API Sablonu", FieldCount = 10, LastUsed = "16.03.2026", Format = "API" });

            TotalCount = Templates.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Sablonlar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CreateTemplate()
    {
        IsEditing = true;
        EditTemplateName = string.Empty;
        EditMappings.Clear();
        EditMappings.Add(new ImportFieldMappingDto { SourceColumn = "urun_adi", TargetField = "Name" });
        EditMappings.Add(new ImportFieldMappingDto { SourceColumn = "fiyat", TargetField = "Price" });
        EditMappings.Add(new ImportFieldMappingDto { SourceColumn = "stok", TargetField = "Stock" });
        EditMappings.Add(new ImportFieldMappingDto { SourceColumn = "barkod", TargetField = "Barcode" });
    }

    [RelayCommand]
    private void EditTemplate(ImportTemplateDto? template)
    {
        if (template is null) return;
        IsEditing = true;
        EditTemplateName = template.Name;
        EditMappings.Clear();
        // Load sample mappings
        EditMappings.Add(new ImportFieldMappingDto { SourceColumn = "urun_adi", TargetField = "Name" });
        EditMappings.Add(new ImportFieldMappingDto { SourceColumn = "fiyat", TargetField = "Price" });
        EditMappings.Add(new ImportFieldMappingDto { SourceColumn = "stok", TargetField = "Stock" });
    }

    [RelayCommand]
    private void DeleteTemplate(ImportTemplateDto? template)
    {
        if (template is null) return;
        Templates.Remove(template);
        TotalCount = Templates.Count;
        IsEmpty = TotalCount == 0;
    }

    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTemplateName))
        {
            HasError = true;
            ErrorMessage = "Sablon adi zorunludur.";
            return;
        }

        IsSaving = true;
        HasError = false;
        try
        {
            await Task.Delay(400);
            IsEditing = false;
            SaveCompleted = true;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Sablon kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditTemplateName = string.Empty;
        EditMappings.Clear();
    }

    [RelayCommand]
    private void AddFieldMapping()
    {
        EditMappings.Add(new ImportFieldMappingDto());
    }

    [RelayCommand]
    private void RemoveFieldMapping(ImportFieldMappingDto? mapping)
    {
        if (mapping is not null)
            EditMappings.Remove(mapping);
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ImportTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public int FieldCount { get; set; }
    public string LastUsed { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public class ImportFieldMappingDto
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
}
