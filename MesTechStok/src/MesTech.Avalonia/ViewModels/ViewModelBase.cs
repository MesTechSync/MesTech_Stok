using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tüm Avalonia ViewModel'lerin base class'ı.
/// CancellationToken lifecycle + Dispose pattern + SafeExecute.
/// V4: CancellationToken eklendi — view kapandığında async işlemler iptal edilir.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    /// <summary>DbContext concurrency guard — EF Core tek thread zorunluluğu.
    /// Tüm MediatR çağrıları bu semaphore ile serileştirilir.</summary>
    private readonly SemaphoreSlim _dbGuard = new(1, 1);
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
    private bool _isEmpty;

    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>True when content should be visible (not loading, no error, not empty).</summary>
    public bool HasContent => !IsLoading && !HasError && !IsEmpty;

    // ── KN-6 FIX: Triple-state mutual exclusion ─────────────────────
    // IsLoading/HasError/IsEmpty are mutually exclusive.
    // When one goes true, the others become false.
    partial void OnIsLoadingChanged(bool value)
    {
        if (value) { _hasError = false; _isEmpty = false; OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(IsEmpty)); }
        OnPropertyChanged(nameof(HasContent));
    }

    partial void OnHasErrorChanged(bool value)
    {
        if (value) { _isLoading = false; _isEmpty = false; OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(IsEmpty)); }
        OnPropertyChanged(nameof(HasContent));
    }

    partial void OnIsEmptyChanged(bool value)
    {
        if (value) { _isLoading = false; _hasError = false; OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(HasError)); }
        OnPropertyChanged(nameof(HasContent));
    }

    /// <summary>View ilk yüklendiğinde çağrılır. LoadAsync'i çağırır.
    /// DB bağlantısı yokken crash önlemek için try-catch sarmalı.</summary>
    public virtual async Task InitializeAsync()
    {
        try
        {
            await LoadAsync();
        }
        catch (OperationCanceledException)
        {
            // View kapandı — normal
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Veri yuklenemedi: {ex.Message}";
        }
    }

    /// <summary>Ctrl+F kısayolu ile arama TextBox'a focus iste. BaseView handle eder.</summary>
    public event EventHandler? FocusSearchRequested;

    [RelayCommand]
    private void FocusSearch() => FocusSearchRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>AXAML'dan bağlanabilen LoadData komutu. LoadAsync'i çağırır.</summary>
    [RelayCommand]
    private Task LoadDataAsync() => LoadAsync();

    /// <summary>Veri yükleme. Mevcut 124 ViewModel bu metodu kullanıyor.</summary>
    public virtual Task LoadAsync() => Task.CompletedTask;

    /// <summary>Hata durumunu temizle.</summary>
    protected void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    /// <summary>Hata durumunu ayarla. IsLoading ve IsEmpty otomatik false olur (KN-6).</summary>
    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true; // triggers mutual exclusion via OnHasErrorChanged
    }

    /// <summary>
    /// Run action on UI thread if available; fall back to direct call in test context.
    /// </summary>
    private async Task RunOnUIAsync(Func<Task> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            await action();
        }
        else
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(action);
            }
            catch (InvalidOperationException)
            {
                // No Dispatcher available (unit test context) — execute directly
                await action();
            }
        }
    }

    private async Task RunOnUIAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(action);
            }
            catch (InvalidOperationException)
            {
                action();
            }
        }
    }

    /// <summary>API çağrısını try-catch + DbContext concurrency guard ile sarmala.</summary>
    protected async Task SafeExecuteAsync(Func<Task> action, string context = "")
    {
        await _dbGuard.WaitAsync(CancellationToken).ConfigureAwait(false);
        try
        {
            // KÖK-4 FIX: ObservableCollection mutations + IsLoading must run on UI thread.
            // action() may contain .Clear()/.Add() on ObservableCollections.
            await RunOnUIAsync(async () =>
            {
                IsLoading = true;
                ClearError();
                await action().ConfigureAwait(false);
            });
        }
        catch (OperationCanceledException)
        {
            // View kapandı — normal, sessizce çık
        }
        catch (Exception ex)
        {
            await RunOnUIAsync(() => SetError($"{context}: {ex.Message}"));
        }
        finally
        {
            await RunOnUIAsync(() => IsLoading = false);
            _dbGuard.Release();
        }
    }

    /// <summary>CancellationToken destekli SafeExecute + DbContext concurrency guard.</summary>
    protected async Task SafeExecuteAsync(Func<CancellationToken, Task> action, string context = "")
    {
        await _dbGuard.WaitAsync(CancellationToken).ConfigureAwait(false);
        try
        {
            await RunOnUIAsync(async () =>
            {
                IsLoading = true;
                ClearError();
                await action(CancellationToken).ConfigureAwait(false);
            });
        }
        catch (OperationCanceledException)
        {
            // View kapandı — normal
        }
        catch (Exception ex)
        {
            await RunOnUIAsync(() => SetError($"{context}: {ex.Message}"));
        }
        finally
        {
            await RunOnUIAsync(() => IsLoading = false);
            _dbGuard.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _cts.Dispose();
        _dbGuard.Dispose();

        OnDispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Override: Alt sınıf kaynaklarını temizle (timer, event, stream).</summary>
    protected virtual void OnDispose() { }
}
