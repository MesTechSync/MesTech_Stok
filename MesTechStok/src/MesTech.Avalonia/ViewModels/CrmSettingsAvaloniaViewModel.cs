using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Avalonia.Services;

namespace MesTech.Avalonia.ViewModels;

public partial class CrmSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
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

    public CrmSettingsAvaloniaViewModel(IMediator mediator, IDialogService dialog)
    {
        _mediator = mediator;
        _dialog = dialog;
    }

    public override Task LoadAsync()
    {
        // TODO: Wire to GetCrmSettingsQuery when CRM settings CQRS is implemented
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
            // TODO: Wire to SaveCrmSettingsCommand when CRM settings CQRS is implemented
            await _dialog.ShowInfoAsync("CRM ayarlari kaydedildi.", "MesTech CRM");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
