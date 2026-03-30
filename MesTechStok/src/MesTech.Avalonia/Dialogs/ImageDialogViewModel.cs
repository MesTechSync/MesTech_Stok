using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.Dialogs;

/// <summary>ViewModel for ImageDialog — image gallery display.</summary>
public partial class ImageDialogViewModel : ObservableObject
{
    [ObservableProperty] private string title = "Resim Galerisi";
}
