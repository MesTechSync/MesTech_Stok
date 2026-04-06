using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

public partial class AboutAvaloniaViewModel : ViewModelBase
{

    [ObservableProperty] private string versionText = "v1.0.0";
    [ObservableProperty] private string platformText = string.Empty;
    [ObservableProperty] private string licenseText = "Ticari Lisans";
    [ObservableProperty] private string copyrightText = string.Empty;

    public AboutAvaloniaViewModel()
    {
        PlatformText = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
        CopyrightText = $"© {DateTime.Now.Year} MesTech Yazilim. Tum haklari saklidir.";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(_ =>
        {
            var asm = typeof(AboutAvaloniaViewModel).Assembly;
            var ver = asm.GetName().Version;
            if (ver is not null)
                VersionText = $"v{ver.Major}.{ver.Minor}.{ver.Build}";
            return Task.CompletedTask;
        }, "Hakkinda bilgileri yuklenirken hata");
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}
