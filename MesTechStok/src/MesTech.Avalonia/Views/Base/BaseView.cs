using Avalonia;
using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views.Base;

/// <summary>
/// Tüm view'ların base class'ı. Avalonia lifecycle hook'ları ile
/// otomatik event subscribe/unsubscribe + ViewModel.InitializeAsync çağrısı.
/// Event handler leak'i SIFIR.
/// V4: ENT-DEV2-INSA-ATLASI — yapısal borç kapatma.
/// </summary>
public abstract class BaseView : UserControl
{
    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeEvents();

        // ViewModel LoadAsync otomatik çağrısı — async void crash koruması
        try
        {
            if (DataContext is ViewModelBase vmBase)
            {
                await vmBase.InitializeAsync();
            }
            else if (DataContext is ILoadable loadable)
            {
                await loadable.LoadAsync();
            }
        }
        catch (Exception ex)
        {
            // DB bağlantısı yokken crash önleme — hata ViewModel'e yansıtılır
            System.Diagnostics.Debug.WriteLine($"[BaseView] InitializeAsync failed: {ex.Message}");
            if (DataContext is ViewModelBase vm)
            {
                vm.HasError = true;
                vm.ErrorMessage = $"Veri yuklenemedi: {ex.Message}";
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeEvents();

        // ViewModel Dispose — CancellationToken cancel + kaynak temizliği
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>Override: Event handler'ları bağla (+=)</summary>
    protected virtual void SubscribeEvents()
    {
        if (DataContext is ViewModelBase vm)
            vm.FocusSearchRequested += OnFocusSearchRequested;
    }

    /// <summary>Override: Event handler'ları çöz (-=)</summary>
    protected virtual void UnsubscribeEvents()
    {
        if (DataContext is ViewModelBase vm)
            vm.FocusSearchRequested -= OnFocusSearchRequested;
    }

    /// <summary>Ctrl+F kısayolu: x:Name="SearchBox" olan TextBox'a focus ver.</summary>
    private void OnFocusSearchRequested(object? sender, EventArgs e)
    {
        var searchBox = this.FindControl<TextBox>("SearchBox");
        searchBox?.Focus();
    }
}
