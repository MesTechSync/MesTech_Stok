using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Settings.Commands.SaveImportTemplate;
using MesTech.Application.Features.Settings.Queries.GetImportSettings;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Icerik Aktarma Ayarlari ViewModel — kolon eslestirme sablonlari yonetimi.
/// </summary>
public partial class ImportSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // Edit mode
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string editTemplateName = string.Empty;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool saveCompleted;

    public ObservableCollection<ImportTemplateDisplayItem> Templates { get; } = [];
    public ObservableCollection<ImportFieldMappingDisplayItem> EditMappings { get; } = [];

    public ImportSettingsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetImportSettingsQuery(_currentUser.TenantId));

            Templates.Clear();
            foreach (var t in result.Templates)
            {
                Templates.Add(new ImportTemplateDisplayItem
                {
                    Name = t.Name,
                    FieldCount = t.FieldCount,
                    LastUsed = t.LastUsedAt?.ToString("dd.MM.yyyy") ?? "—",
                    Format = t.Format
                });
            }

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
        EditMappings.Add(new ImportFieldMappingDisplayItem { SourceColumn = "urun_adi", TargetField = "Name" });
        EditMappings.Add(new ImportFieldMappingDisplayItem { SourceColumn = "fiyat", TargetField = "Price" });
        EditMappings.Add(new ImportFieldMappingDisplayItem { SourceColumn = "stok", TargetField = "Stock" });
        EditMappings.Add(new ImportFieldMappingDisplayItem { SourceColumn = "barkod", TargetField = "Barcode" });
    }

    [RelayCommand]
    private void EditTemplate(ImportTemplateDisplayItem? template)
    {
        if (template is null) return;
        IsEditing = true;
        EditTemplateName = template.Name;
        EditMappings.Clear();
        EditMappings.Add(new ImportFieldMappingDisplayItem { SourceColumn = "urun_adi", TargetField = "Name" });
        EditMappings.Add(new ImportFieldMappingDisplayItem { SourceColumn = "fiyat", TargetField = "Price" });
        EditMappings.Add(new ImportFieldMappingDisplayItem { SourceColumn = "stok", TargetField = "Stock" });
    }

    [RelayCommand]
    private void DeleteTemplate(ImportTemplateDisplayItem? template)
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
            var mappings = EditMappings.ToDictionary(m => m.SourceColumn, m => m.TargetField);
            await _mediator.Send(new SaveImportTemplateCommand(
                _currentUser.TenantId,
                EditTemplateName,
                "CSV",
                mappings));

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
        EditMappings.Add(new ImportFieldMappingDisplayItem());
    }

    [RelayCommand]
    private void RemoveFieldMapping(ImportFieldMappingDisplayItem? mapping)
    {
        if (mapping is not null)
            EditMappings.Remove(mapping);
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}

public class ImportTemplateDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public int FieldCount { get; set; }
    public string LastUsed { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public class ImportFieldMappingDisplayItem
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
}
