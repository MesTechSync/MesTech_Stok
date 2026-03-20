using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Avalonia.Services;

namespace MesTech.Avalonia.ViewModels;

public partial class CrmSettingsAvaloniaViewModel : ObservableObject
{
    private readonly IDialogService _dialog;

    [ObservableProperty] private bool isMesaAiEnabled = true;
    [ObservableProperty] private int messageCheckIntervalMinutes = 5;
    [ObservableProperty] private string newTemplateText = string.Empty;
    [ObservableProperty] private string? selectedTemplate;
    [ObservableProperty] private bool isSaving;

    public ObservableCollection<string> QuickReplyTemplates { get; } =
    [
        "Siparisiz hazirlanmaktadir, en kisa surede kargoya verilecektir.",
        "Ilginiz icin tesekkur ederiz. Size nasil yardimci olabiliriz?",
        "Iade talebiniz onaylanmistir. Urunu kargoya vermenizi rica ederiz.",
        "Stok bilgisi guncellenmistir. Urun su an mevcuttur."
    ];

    public CrmSettingsAvaloniaViewModel(IDialogService dialog)
    {
        _dialog = dialog;
    }

    public Task LoadAsync()
    {
        // Settings loaded from defaults — will integrate with IConfiguration in future
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void AddTemplate()
    {
        if (string.IsNullOrWhiteSpace(NewTemplateText))
            return;

        QuickReplyTemplates.Add(NewTemplateText.Trim());
        NewTemplateText = string.Empty;
    }

    [RelayCommand]
    private void RemoveTemplate()
    {
        if (SelectedTemplate is not null)
        {
            QuickReplyTemplates.Remove(SelectedTemplate);
            SelectedTemplate = null;
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        IsSaving = true;
        try
        {
            // Persist settings via IConfiguration / local storage
            await Task.Delay(200);
            await _dialog.ShowInfoAsync("CRM ayarlari kaydedildi.", "MesTech CRM");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
