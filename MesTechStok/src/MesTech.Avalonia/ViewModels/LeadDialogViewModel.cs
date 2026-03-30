using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.ViewModels;

/// <summary>ViewModel for LeadDialog — customer lead entry form.</summary>
public partial class LeadDialogViewModel : ObservableObject
{
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string source = "Web Sitesi";
    [ObservableProperty] private string note = string.Empty;
}
