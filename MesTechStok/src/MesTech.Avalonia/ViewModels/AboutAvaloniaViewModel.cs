#pragma warning disable CS1998
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class AboutAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string versionText = "v1.0.0";
    [ObservableProperty] private string platformText = string.Empty;
    [ObservableProperty] private string licenseText = "Ticari Lisans";
    [ObservableProperty] private string copyrightText = string.Empty;

    public AboutAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        PlatformText = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
        CopyrightText = $"© {DateTime.Now.Year} MesTech Yazilim. Tum haklari saklidir.";
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // Version and license info loaded from assembly metadata
            var asm = typeof(AboutAvaloniaViewModel).Assembly;
            var ver = asm.GetName().Version;
            if (ver is not null)
                VersionText = $"v{ver.Major}.{ver.Minor}.{ver.Build}";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Bilgiler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
