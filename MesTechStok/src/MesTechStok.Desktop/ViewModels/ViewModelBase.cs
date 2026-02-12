using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTechStok.Desktop.ViewModels;

/// <summary>
/// Tüm ViewModel sınıflarının türeyeceği temel sınıf
/// MVVM pattern için gerekli temel işlevselliği sağlar
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    /// <summary>
    /// ViewModel başlatma işlemleri için
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Veri yenileme işlemleri için
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;
            StatusMessage = "Yenileniyor...";

            await OnRefreshAsync();

            StatusMessage = "Başarıyla yenilendi";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusMessage = "Yenileme başarısız";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected virtual Task OnRefreshAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
        StatusMessage = "Hata oluştu";
    }

    protected virtual void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    protected virtual void SetStatus(string message)
    {
        StatusMessage = message;
        HasError = false;
    }

    protected async Task ExecuteWithLoadingAsync(Func<Task> operation, string? loadingMessage = null)
    {
        try
        {
            IsLoading = true;
            StatusMessage = loadingMessage ?? "İşlem yapılıyor...";
            ClearError();

            await operation();

            StatusMessage = "İşlem tamamlandı";
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task<T?> ExecuteWithLoadingAsync<T>(Func<Task<T>> operation, string? loadingMessage = null)
    {
        try
        {
            IsLoading = true;
            StatusMessage = loadingMessage ?? "İşlem yapılıyor...";
            ClearError();

            var result = await operation();

            StatusMessage = "İşlem tamamlandı";
            return result;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }
}