using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Commands.SaveCrmSettings;
using MesTech.Application.Features.Crm.Queries.GetCrmSettings;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class CrmSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private bool isMesaAiEnabled = true;
    [ObservableProperty] private int messageCheckIntervalMinutes = 5;
    [ObservableProperty] private string newTemplateText = string.Empty;
    [ObservableProperty] private string? selectedTemplate;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool autoAssignLeads;
    [ObservableProperty] private int leadScoreThreshold = 50;
    [ObservableProperty] private bool enableEmailTracking;

    public ObservableCollection<string> QuickReplyTemplates { get; } =
    [
        "Siparisiz hazirlanmaktadir, en kisa surede kargoya verilecektir.",
        "Ilginiz icin tesekkur ederiz. Size nasil yardimci olabiliriz?",
        "Iade talebiniz onaylanmistir. Urunu kargoya vermenizi rica ederiz.",
        "Stok bilgisi guncellenmistir. Urun su an mevcuttur."
    ];

    public CrmSettingsAvaloniaViewModel(IMediator mediator, IDialogService dialog, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _dialog = dialog;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var settings = await _mediator.Send(new GetCrmSettingsQuery(_currentUser.TenantId));
            AutoAssignLeads = settings.AutoAssignLeads;
            LeadScoreThreshold = settings.LeadScoreThreshold;
            EnableEmailTracking = settings.EnableEmailTracking;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"CRM ayarlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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
            var result = await _mediator.Send(new SaveCrmSettingsCommand(
                _currentUser.TenantId,
                AutoAssignLeads,
                null,
                LeadScoreThreshold,
                EnableEmailTracking));
            if (result.IsSuccess)
                await _dialog.ShowInfoAsync("CRM ayarlari kaydedildi.", "MesTech CRM");
            else
                await _dialog.ShowInfoAsync($"Kaydetme hatasi: {result.ErrorMessage}", "MesTech CRM");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
