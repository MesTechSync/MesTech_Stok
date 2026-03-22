namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// View attach olduğunda otomatik LoadAsync çağrısı için interface.
/// BaseView.OnAttachedToVisualTree bu interface'i kontrol eder.
/// 124 ViewModel zaten LoadAsync() metodu var — bu interface'i implement etmeleri yeterli.
/// </summary>
public interface ILoadable
{
    Task LoadAsync();
}
