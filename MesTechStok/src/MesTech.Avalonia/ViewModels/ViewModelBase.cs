using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tüm Avalonia ViewModel'lerin base class'ı.
/// WPF ViewModelBase ile aynı pattern — CommunityToolkit.Mvvm.
/// Mevcut 80 ViewModel ObservableObject'ten direkt türüyor;
/// yeni ViewModel'ler bu base class'ı kullanabilir.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>View ilk yüklendiğinde çağrılır.</summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>Hata durumunu temizle.</summary>
    protected void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    /// <summary>Hata durumunu ayarla.</summary>
    protected void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
    }

    /// <summary>API çağrısını try-catch ile sarmala.</summary>
    protected async Task SafeExecuteAsync(Func<Task> action, string context = "")
    {
        try
        {
            IsLoading = true;
            ClearError();
            await action();
        }
        catch (Exception ex)
        {
            SetError($"{context}: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public virtual void Dispose() { }
}
