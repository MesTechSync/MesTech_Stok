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

        // ViewModel varsa otomatik LoadAsync/InitializeAsync çağır
        if (DataContext is ViewModelBase vmBase)
        {
            await vmBase.InitializeAsync();
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
    protected virtual void SubscribeEvents() { }

    /// <summary>Override: Event handler'ları çöz (-=)</summary>
    protected virtual void UnsubscribeEvents() { }
}
