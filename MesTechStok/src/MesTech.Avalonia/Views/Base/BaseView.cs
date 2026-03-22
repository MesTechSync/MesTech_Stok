using Avalonia;
using Avalonia.Controls;

namespace MesTech.Avalonia.Views.Base;

/// <summary>
/// Tüm view'ların base class'ı. Avalonia lifecycle hook'ları ile
/// otomatik event subscribe/unsubscribe. Event handler leak'i SIFIR.
/// V4: ENT-DEV2-INSA-ATLASI — yapısal borç kapatma.
/// </summary>
public abstract class BaseView : UserControl
{
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeEvents();
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
