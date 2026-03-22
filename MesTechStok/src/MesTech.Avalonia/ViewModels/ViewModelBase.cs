using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tüm Avalonia ViewModel'lerin base class'ı.
/// CancellationToken lifecycle + Dispose pattern + SafeExecute.
/// V4: CancellationToken eklendi — view kapandığında async işlemler iptal edilir.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    /// <summary>ViewModel yaşam döngüsü boyunca kullanılacak CancellationToken.
    /// View kapandığında otomatik cancel edilir.</summary>
    protected CancellationToken CancellationToken => _cts.Token;

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

    /// <summary>API çağrısını try-catch ile sarmala. CancellationToken destekli.</summary>
    protected async Task SafeExecuteAsync(Func<Task> action, string context = "")
    {
        try
        {
            IsLoading = true;
            ClearError();
            await action();
        }
        catch (OperationCanceledException)
        {
            // View kapandı — normal, sessizce çık
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

    /// <summary>CancellationToken destekli SafeExecute overload.</summary>
    protected async Task SafeExecuteAsync(Func<CancellationToken, Task> action, string context = "")
    {
        try
        {
            IsLoading = true;
            ClearError();
            await action(CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // View kapandı — normal
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _cts.Dispose();

        OnDispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Override: Alt sınıf kaynaklarını temizle (timer, event, stream).</summary>
    protected virtual void OnDispose() { }
}
